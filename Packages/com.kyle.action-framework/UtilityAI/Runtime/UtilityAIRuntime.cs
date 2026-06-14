using System;
using System.Collections.Generic;

namespace UtilityAI
{
    /// <summary>
    /// UtilityAI Runtime 创建入口，负责校验配置并缓存 Handler。
    /// </summary>
    public static class UtilityAIRuntime
    {
        public static bool TryCreate<TContext>(
            UtilityAIConfig config,
            UtilityActionRegistry<TContext> registry,
            out UtilityAIRuntime<TContext> runtime)
            where TContext : IUtilityIntentState, IUtilitySupportConstraintProvider
        {
            runtime = null;
            if (config == null || registry == null)
                return false;

            if (config.TerminalActions == null || config.TerminalActions.Length == 0)
                return false;

            if (config.MaxSupportCount < 0 ||
                config.RepositionMargin < 0 ||
                config.IntentStickiness < 0 ||
                config.PreparationTimeoutTicks < 0 ||
                !IsFinite(config.RepositionMargin) ||
                !IsFinite(config.IntentStickiness))
                return false;

            var terminalActions = new TerminalActionRuntime<TContext>[config.TerminalActions.Length];
            for (int i = 0; i < config.TerminalActions.Length; i++)
            {
                var terminalConfig = config.TerminalActions[i];
                if (terminalConfig == null)
                    return false;

                ITerminalActionInvoker<TContext> handler;
                if (!registry.TryGetTerminalHandler(terminalConfig.GetType(), out handler))
                    return false;

                terminalActions[i] = new TerminalActionRuntime<TContext>
                {
                    Config = terminalConfig,
                    Handler = handler,
                    LastSelectedTick = -1
                };
            }

            var supportConfigs = config.SupportActions ?? Array.Empty<SupportActionConfig>();
            var supportActions = new SupportActionRuntime<TContext>[supportConfigs.Length];
            for (int i = 0; i < supportConfigs.Length; i++)
            {
                var supportConfig = supportConfigs[i];
                if (supportConfig == null || supportConfig.MinHoldTicks < 0)
                    return false;

                if (!ValidateChannels(supportConfig.Channels))
                    return false;

                ISupportActionInvoker<TContext> handler;
                if (!registry.TryGetSupportHandler(supportConfig.GetType(), out handler))
                    return false;

                supportActions[i] = new SupportActionRuntime<TContext>
                {
                    Config = supportConfig,
                    Handler = handler
                };
            }

            runtime = new UtilityAIRuntime<TContext>(config, terminalActions, supportActions);
            return true;
        }

        private static bool ValidateChannels(UtilitySupportChannel[] channels)
        {
            if (channels == null || channels.Length == 0)
                return false;

            for (int i = 0; i < channels.Length; i++)
            {
                if (!IsValidChannel(channels[i]))
                    return false;

                for (int j = i + 1; j < channels.Length; j++)
                {
                    if (channels[i] == channels[j])
                        return false;
                }
            }

            return true;
        }

        private static bool IsValidChannel(UtilitySupportChannel channel)
        {
            switch (channel)
            {
                case UtilitySupportChannel.Movement:
                case UtilitySupportChannel.Facing:
                case UtilitySupportChannel.Positioning:
                case UtilitySupportChannel.Validation:
                case UtilitySupportChannel.Modifier:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }

    /// <summary>
    /// UtilityAI 决策运行时，驱动 Evaluate 和 Execute 两阶段流程。
    /// </summary>
    public sealed class UtilityAIRuntime<TContext>
        where TContext : IUtilityIntentState, IUtilitySupportConstraintProvider
    {
        private struct TerminalCandidate
        {
            public int Index;
            public UtilityIntentMode Mode;
            public TerminalScore Score;
            public float EffectiveScore;
        }

        private struct SupportCandidate
        {
            public int Index;
            public bool IsRequired;
            public bool HasMinHold;
            public SupportScore Score;
        }

        private struct BuiltSupport
        {
            public int Index;
            public bool IsRequired;
            public SupportScore Score;
        }

        private sealed class TerminalCandidateComparer : IComparer<TerminalCandidate>
        {
            public static readonly TerminalCandidateComparer Instance = new TerminalCandidateComparer();

            public int Compare(TerminalCandidate left, TerminalCandidate right)
            {
                int scoreCompare = right.EffectiveScore.CompareTo(left.EffectiveScore);
                if (scoreCompare != 0)
                    return scoreCompare;
                return left.Index.CompareTo(right.Index);
            }
        }

        private sealed class SupportCandidateComparer : IComparer<SupportCandidate>
        {
            public static readonly SupportCandidateComparer Instance = new SupportCandidateComparer();

            public int Compare(SupportCandidate left, SupportCandidate right)
            {
                if (left.IsRequired != right.IsRequired)
                    return left.IsRequired ? -1 : 1;

                if (left.HasMinHold != right.HasMinHold)
                    return left.HasMinHold ? -1 : 1;

                int scoreCompare = right.Score.Score.CompareTo(left.Score.Score);
                if (scoreCompare != 0)
                    return scoreCompare;

                return left.Index.CompareTo(right.Index);
            }
        }

        private sealed class BuiltSupportComparer : IComparer<BuiltSupport>
        {
            public static readonly BuiltSupportComparer Instance = new BuiltSupportComparer();

            public int Compare(BuiltSupport left, BuiltSupport right)
            {
                if (left.IsRequired != right.IsRequired)
                    return left.IsRequired ? -1 : 1;
                return left.Index.CompareTo(right.Index);
            }
        }

        private const int NoTerminal = -1;

        public readonly UtilityAIConfig Config;
        public readonly TerminalActionRuntime<TContext>[] TerminalActions;
        public readonly SupportActionRuntime<TContext>[] SupportActions;

        private readonly int[] _selectedSupportIndices;
        private readonly bool[] _selectedSupportRequired;
        private readonly int[] _prepareStartTicks;
        private readonly List<TerminalCandidate> _terminalCandidates;
        private readonly List<SupportCandidate> _supportCandidates;
        private readonly List<SupportCandidate> _allowedSupportCandidates;
        private readonly BuiltSupport[] _builtSupports;
        private readonly bool[] _singleOwnerOccupied;
        private readonly bool[] _excludedAllowedSupportBuildFailures;

        private int _currentDecisionVersion;
        private int _lastExecutedDecisionVersion = -1;
        private int _currentSelectedSupportCount;
        private UtilityDecisionResult _currentDecision;
        private bool _hasCurrentDecision;
        private int _committedTerminalIndex = NoTerminal;
        private int _commitStartTick = -1;
        private int _commitUntilTick = -1;
        private int _previousNormalTerminalIndex = NoTerminal;
        private UtilityIntentMode _previousNormalMode = UtilityIntentMode.None;

        internal UtilityAIRuntime(
            UtilityAIConfig config,
            TerminalActionRuntime<TContext>[] terminalActions,
            SupportActionRuntime<TContext>[] supportActions)
        {
            Config = config;
            TerminalActions = terminalActions;
            SupportActions = supportActions;
            _selectedSupportIndices = new int[Math.Max(1, supportActions.Length)];
            _selectedSupportRequired = new bool[Math.Max(1, supportActions.Length)];
            _prepareStartTicks = new int[terminalActions.Length];
            _terminalCandidates = new List<TerminalCandidate>(terminalActions.Length);
            _supportCandidates = new List<SupportCandidate>(supportActions.Length);
            _allowedSupportCandidates = new List<SupportCandidate>(supportActions.Length);
            _builtSupports = new BuiltSupport[Math.Max(1, supportActions.Length)];
            _singleOwnerOccupied = new bool[3];
            _excludedAllowedSupportBuildFailures = new bool[Math.Max(1, supportActions.Length)];
            for (int i = 0; i < _prepareStartTicks.Length; i++)
                _prepareStartTicks[i] = -1;
        }

        public UtilityDecisionResult Evaluate(TContext context, UtilityTickInfo tickInfo)
        {
            _currentDecisionVersion++;
            ClearSelectedSupports();
            context.ResetUtilityIntentState();

            if (_committedTerminalIndex != NoTerminal)
            {
                if (IsCommittedFinished(context, tickInfo))
                {
                    ClearCommitted();
                    context.ResetUtilityIntentState();
                }
                else
                {
                    if (CanCommittedBeInterrupted(context, tickInfo))
                    {
                        UtilityDecisionResult emergencyDecision;
                        if (TrySelectEmergency(context, tickInfo, out emergencyDecision))
                            return emergencyDecision;
                    }

                    UtilityDecisionResult committedDecision;
                    if (TrySelectCommitted(context, tickInfo, out committedDecision))
                        return committedDecision;

                    ClearCommitted();
                    context.ResetUtilityIntentState();
                }
            }

            UtilityDecisionResult nonCommittedEmergencyDecision;
            if (TrySelectEmergency(context, tickInfo, out nonCommittedEmergencyDecision))
                return nonCommittedEmergencyDecision;

            UtilityDecisionResult normalDecision;
            if (TrySelectNormal(context, tickInfo, out normalDecision))
                return normalDecision;

            ClearSupportActiveStates();
            _previousNormalTerminalIndex = NoTerminal;
            _previousNormalMode = UtilityIntentMode.None;
            return CreateDecision(UtilityIntentMode.None, NoTerminal, 0);
        }

        public UtilityExecutionResult Execute(TContext context, UtilityDecisionResult decision, UtilityTickInfo tickInfo)
        {
            if (decision.DecisionVersion != _currentDecisionVersion ||
                !_hasCurrentDecision ||
                decision.DecisionVersion == _lastExecutedDecisionVersion)
                return new UtilityExecutionResult { Status = UtilityExecutionStatus.Rejected };

            if (!IsKnownMode(decision.Mode) || !MatchesCurrentDecision(decision))
                return RejectCurrentDecision(decision);

            if (decision.Mode == UtilityIntentMode.None)
            {
                _lastExecutedDecisionVersion = decision.DecisionVersion;
                return new UtilityExecutionResult { Status = UtilityExecutionStatus.Success };
            }

            if (decision.SelectedTerminalIndex < 0 || decision.SelectedTerminalIndex >= TerminalActions.Length)
                return RejectCurrentDecision(decision);

            if (decision.SelectedSupportCount != _currentSelectedSupportCount)
                return RejectCurrentDecision(decision);

            if (decision.Mode == UtilityIntentMode.Execute || decision.Mode == UtilityIntentMode.Emergency)
            {
                if (decision.Mode == UtilityIntentMode.Emergency)
                    ClearCommitted();

                var terminalStatus = ExecuteTerminal(context, decision, tickInfo);
                if (terminalStatus != UtilityActionExecutionStatus.Success)
                {
                    ClearSupportActiveStates();
                    _lastExecutedDecisionVersion = decision.DecisionVersion;
                    return new UtilityExecutionResult { Status = ToExecutionStatus(terminalStatus) };
                }

                var terminalRuntime = TerminalActions[decision.SelectedTerminalIndex];
                if (terminalRuntime.LastScore.LockTicks > 0)
                    SetCommitted(decision.SelectedTerminalIndex, tickInfo.TickIndex, terminalRuntime.LastScore.LockTicks);
                else if (decision.Mode == UtilityIntentMode.Emergency)
                    ClearCommitted();
            }

            var supportStatus = ExecuteSupports(context, decision, tickInfo);
            _lastExecutedDecisionVersion = decision.DecisionVersion;
            return new UtilityExecutionResult { Status = supportStatus };
        }

        public int GetSelectedSupportIndex(int slot)
        {
            if (slot < 0 || slot >= _currentSelectedSupportCount)
                return -1;
            return _selectedSupportIndices[slot];
        }

        private UtilityExecutionResult RejectCurrentDecision(UtilityDecisionResult decision)
        {
            _lastExecutedDecisionVersion = decision.DecisionVersion;
            return new UtilityExecutionResult { Status = UtilityExecutionStatus.Rejected };
        }

        private bool TrySelectNormal(TContext context, UtilityTickInfo tickInfo, out UtilityDecisionResult decision)
        {
            var candidates = _terminalCandidates;
            candidates.Clear();
            for (int i = 0; i < TerminalActions.Length; i++)
            {
                var runtime = TerminalActions[i];
                var score = runtime.Handler.Score(context, runtime.Config, new TerminalScoreInput
                {
                    TickInfo = tickInfo,
                    TerminalIndex = i,
                    DecisionVersion = _currentDecisionVersion,
                    HasCommittedTerminal = false,
                    CommittedTerminalIndex = NoTerminal
                });

                runtime.LastScore = score;
                TerminalActions[i] = runtime;

                TerminalCandidate candidate;
                if (TryCreateNormalCandidate(i, score, tickInfo, out candidate))
                    candidates.Add(candidate);
            }

            candidates.Sort(TerminalCandidateComparer.Instance);
            for (int i = 0; i < candidates.Count; i++)
            {
                if (TryBuildTerminalCandidate(context, tickInfo, candidates[i], out decision))
                {
                    SetNormalSelectionMemory(candidates[i], tickInfo);
                    return true;
                }
            }

            decision = default(UtilityDecisionResult);
            return false;
        }

        private bool TrySelectEmergency(TContext context, UtilityTickInfo tickInfo, out UtilityDecisionResult decision)
        {
            var candidates = _terminalCandidates;
            candidates.Clear();
            for (int i = 0; i < TerminalActions.Length; i++)
            {
                if (i == _committedTerminalIndex)
                    continue;

                var runtime = TerminalActions[i];
                bool hasCommittedTerminal = _committedTerminalIndex != NoTerminal;
                var score = runtime.Handler.Score(context, runtime.Config, new TerminalScoreInput
                {
                    TickInfo = tickInfo,
                    TerminalIndex = i,
                    DecisionVersion = _currentDecisionVersion,
                    HasCommittedTerminal = hasCommittedTerminal,
                    CommittedTerminalIndex = _committedTerminalIndex
                });

                runtime.LastScore = score;
                TerminalActions[i] = runtime;

                if (!score.IsEmergency || !IsFinite(score.EmergencyScore))
                    continue;

                candidates.Add(new TerminalCandidate
                {
                    Index = i,
                    Mode = UtilityIntentMode.Emergency,
                    Score = score,
                    EffectiveScore = score.EmergencyScore
                });
            }

            candidates.Sort(TerminalCandidateComparer.Instance);
            for (int i = 0; i < candidates.Count; i++)
            {
                if (TryBuildTerminalCandidate(context, tickInfo, candidates[i], out decision))
                    return true;
            }

            decision = default(UtilityDecisionResult);
            return false;
        }

        private bool TrySelectCommitted(TContext context, UtilityTickInfo tickInfo, out UtilityDecisionResult decision)
        {
            var runtime = TerminalActions[_committedTerminalIndex];
            context.ResetUtilityIntentState();
            var buildStatus = runtime.Handler.BuildIntent(context, runtime.Config, new TerminalIntentBuildInput
            {
                TickInfo = tickInfo,
                TerminalIndex = _committedTerminalIndex,
                DecisionVersion = _currentDecisionVersion,
                Mode = UtilityIntentMode.Committed,
                Score = runtime.LastScore
            });

            if (buildStatus != UtilityIntentBuildStatus.Success)
            {
                decision = default(UtilityDecisionResult);
                return false;
            }

            int builtCount;
            if (!TryBuildSupports(context, tickInfo, _committedTerminalIndex, UtilityIntentMode.Committed, out builtCount))
            {
                decision = default(UtilityDecisionResult);
                return false;
            }

            ApplySelectedSupports(tickInfo, builtCount);
            MarkTerminalSelected(_committedTerminalIndex, tickInfo.TickIndex);
            decision = CreateDecision(UtilityIntentMode.Committed, _committedTerminalIndex, builtCount);
            return true;
        }

        private bool TryBuildTerminalCandidate(
            TContext context,
            UtilityTickInfo tickInfo,
            TerminalCandidate candidate,
            out UtilityDecisionResult decision)
        {
            context.ResetUtilityIntentState();

            var runtime = TerminalActions[candidate.Index];
            var buildStatus = runtime.Handler.BuildIntent(context, runtime.Config, new TerminalIntentBuildInput
            {
                TickInfo = tickInfo,
                TerminalIndex = candidate.Index,
                DecisionVersion = _currentDecisionVersion,
                Mode = candidate.Mode,
                Score = candidate.Score
            });

            if (buildStatus != UtilityIntentBuildStatus.Success)
            {
                decision = default(UtilityDecisionResult);
                return false;
            }

            int builtCount;
            if (!TryBuildSupports(context, tickInfo, candidate.Index, candidate.Mode, out builtCount))
            {
                decision = default(UtilityDecisionResult);
                return false;
            }

            ApplySelectedSupports(tickInfo, builtCount);
            MarkTerminalSelected(candidate.Index, tickInfo.TickIndex);
            decision = CreateDecision(candidate.Mode, candidate.Index, builtCount);
            return true;
        }

        private bool TryBuildSupports(
            TContext context,
            UtilityTickInfo tickInfo,
            int terminalIndex,
            UtilityIntentMode mode,
            out int builtCount)
        {
            builtCount = 0;

            if (SupportActions.Length == 0 || Config.MaxSupportCount == 0)
            {
                if (!RequiredSupportsFitEmptySelection(context))
                    return false;
                return true;
            }

            var candidates = _supportCandidates;
            candidates.Clear();
            for (int i = 0; i < SupportActions.Length; i++)
            {
                var supportRuntime = SupportActions[i];
                bool forbidden = context.IsSupportForbidden(supportRuntime.Config, i);
                bool required = context.IsSupportRequired(supportRuntime.Config, i);
                bool allowed = context.IsSupportAllowed(supportRuntime.Config, i);

                if (forbidden)
                {
                    if (required)
                        return false;
                    continue;
                }

                if (!required && !allowed)
                    continue;

                var score = supportRuntime.Handler.Score(context, supportRuntime.Config, new SupportScoreInput
                {
                    TickInfo = tickInfo,
                    SupportIndex = i,
                    SelectedTerminalIndex = terminalIndex,
                    DecisionVersion = _currentDecisionVersion,
                    Mode = mode,
                    IsRequired = required,
                    IsAllowed = allowed
                });

                supportRuntime.LastScore = score;
                SupportActions[i] = supportRuntime;

                if (!score.IsValid || !IsFinite(score.Score))
                {
                    if (required)
                        return false;
                    continue;
                }

                if (required && !score.IsRequiredSatisfied)
                    return false;

                candidates.Add(new SupportCandidate
                {
                    Index = i,
                    IsRequired = required,
                    HasMinHold = supportRuntime.IsActive && tickInfo.TickIndex < supportRuntime.MinHoldUntilTick,
                    Score = score
                });
            }

            ClearExcludedAllowedSupportBuildFailures();
            bool rebuildTerminalIntent = false;
            while (true)
            {
                if (rebuildTerminalIntent)
                {
                    context.ResetUtilityIntentState();
                    if (!BuildTerminalIntent(context, tickInfo, terminalIndex, mode))
                        return false;
                }

                if (!SelectRequiredAndAllowedSupports(candidates, out builtCount))
                    return false;

                int failedAllowedSupportIndex;
                if (BuildSelectedSupportsInOrder(context, tickInfo, terminalIndex, mode, builtCount, out failedAllowedSupportIndex))
                    return true;

                if (failedAllowedSupportIndex < 0)
                    return false;

                _excludedAllowedSupportBuildFailures[failedAllowedSupportIndex] = true;
                rebuildTerminalIntent = true;
            }
        }

        private bool RequiredSupportsFitEmptySelection(TContext context)
        {
            for (int i = 0; i < SupportActions.Length; i++)
            {
                var config = SupportActions[i].Config;
                if (context.IsSupportRequired(config, i))
                    return false;
            }

            return true;
        }

        private bool SelectRequiredAndAllowedSupports(
            List<SupportCandidate> candidates,
            out int selectedCount)
        {
            selectedCount = 0;
            ClearSingleOwnerOccupied();

            for (int i = 0; i < candidates.Count; i++)
            {
                if (!candidates[i].IsRequired)
                    continue;

                if (selectedCount >= Config.MaxSupportCount)
                    return false;

                if (ConflictsWithSingleOwner(candidates[i].Index, _singleOwnerOccupied))
                    return false;

                OccupySingleOwnerChannels(candidates[i].Index, _singleOwnerOccupied);
                _builtSupports[selectedCount++] = new BuiltSupport
                {
                    Index = candidates[i].Index,
                    IsRequired = true,
                    Score = candidates[i].Score
                };
            }

            var allowedCandidates = _allowedSupportCandidates;
            allowedCandidates.Clear();
            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i].IsRequired)
                    continue;

                if (_excludedAllowedSupportBuildFailures[candidates[i].Index])
                    continue;

                allowedCandidates.Add(candidates[i]);
            }

            allowedCandidates.Sort(SupportCandidateComparer.Instance);
            for (int i = 0; i < allowedCandidates.Count; i++)
            {
                var candidate = allowedCandidates[i];

                if (selectedCount >= Config.MaxSupportCount)
                    break;

                if (ConflictsWithSingleOwner(candidate.Index, _singleOwnerOccupied))
                    continue;

                OccupySingleOwnerChannels(candidate.Index, _singleOwnerOccupied);
                _builtSupports[selectedCount++] = new BuiltSupport
                {
                    Index = candidate.Index,
                    IsRequired = false,
                    Score = candidate.Score
                };
            }

            SortBuiltSupports(selectedCount);
            return true;
        }

        private bool BuildSelectedSupportsInOrder(
            TContext context,
            UtilityTickInfo tickInfo,
            int terminalIndex,
            UtilityIntentMode mode,
            int supportCount,
            out int failedAllowedSupportIndex)
        {
            failedAllowedSupportIndex = -1;
            for (int i = 0; i < supportCount; i++)
            {
                var support = _builtSupports[i];
                if (BuildSupportIntent(context, tickInfo, terminalIndex, mode, support))
                    continue;

                if (support.IsRequired)
                    return false;

                failedAllowedSupportIndex = support.Index;
                return false;
            }

            return true;
        }

        private bool BuildSupportIntent(
            TContext context,
            UtilityTickInfo tickInfo,
            int terminalIndex,
            UtilityIntentMode mode,
            BuiltSupport support)
        {
            var runtime = SupportActions[support.Index];
            var status = runtime.Handler.BuildIntent(context, runtime.Config, new SupportIntentBuildInput
            {
                TickInfo = tickInfo,
                SupportIndex = support.Index,
                SelectedTerminalIndex = terminalIndex,
                DecisionVersion = _currentDecisionVersion,
                Mode = mode,
                IsRequired = support.IsRequired,
                Score = support.Score
            });

            return status == UtilityIntentBuildStatus.Success;
        }

        private bool BuildTerminalIntent(TContext context, UtilityTickInfo tickInfo, int terminalIndex, UtilityIntentMode mode)
        {
            var runtime = TerminalActions[terminalIndex];
            var status = runtime.Handler.BuildIntent(context, runtime.Config, new TerminalIntentBuildInput
            {
                TickInfo = tickInfo,
                TerminalIndex = terminalIndex,
                DecisionVersion = _currentDecisionVersion,
                Mode = mode,
                Score = runtime.LastScore
            });

            return status == UtilityIntentBuildStatus.Success;
        }

        private UtilityActionExecutionStatus ExecuteTerminal(TContext context, UtilityDecisionResult decision, UtilityTickInfo tickInfo)
        {
            var runtime = TerminalActions[decision.SelectedTerminalIndex];
            return runtime.Handler.Execute(context, runtime.Config, new TerminalExecuteInput
            {
                TickInfo = tickInfo,
                TerminalIndex = decision.SelectedTerminalIndex,
                DecisionVersion = decision.DecisionVersion,
                Mode = decision.Mode,
                Score = runtime.LastScore
            });
        }

        private UtilityExecutionStatus ExecuteSupports(TContext context, UtilityDecisionResult decision, UtilityTickInfo tickInfo)
        {
            bool hasFailure = false;
            for (int i = 0; i < decision.SelectedSupportCount; i++)
            {
                int supportIndex = _selectedSupportIndices[i];
                var runtime = SupportActions[supportIndex];
                var status = runtime.Handler.Execute(context, runtime.Config, new SupportExecuteInput
                {
                    TickInfo = tickInfo,
                    SupportIndex = supportIndex,
                    SelectedTerminalIndex = decision.SelectedTerminalIndex,
                    DecisionVersion = decision.DecisionVersion,
                    Mode = decision.Mode,
                    IsRequired = _selectedSupportRequired[i],
                    Score = runtime.LastScore
                });

                if (status != UtilityActionExecutionStatus.Success)
                    hasFailure = true;
            }

            return hasFailure ? UtilityExecutionStatus.PartialFailed : UtilityExecutionStatus.Success;
        }

        private bool IsCommittedFinished(TContext context, UtilityTickInfo tickInfo)
        {
            var runtime = TerminalActions[_committedTerminalIndex];
            return runtime.Handler.IsCommitFinished(context, runtime.Config, new TerminalCommitInput
            {
                TickInfo = tickInfo,
                TerminalIndex = _committedTerminalIndex,
                DecisionVersion = _currentDecisionVersion,
                CommitStartTick = _commitStartTick,
                CommitUntilTick = _commitUntilTick,
                Score = runtime.LastScore
            });
        }

        private bool CanCommittedBeInterrupted(TContext context, UtilityTickInfo tickInfo)
        {
            var runtime = TerminalActions[_committedTerminalIndex];
            return runtime.Handler.CanBeInterrupted(context, runtime.Config, new TerminalInterruptInput
            {
                TickInfo = tickInfo,
                TerminalIndex = _committedTerminalIndex,
                DecisionVersion = _currentDecisionVersion,
                CommitStartTick = _commitStartTick,
                CommitUntilTick = _commitUntilTick,
                Score = runtime.LastScore
            });
        }

        private bool TryCreateNormalCandidate(int index, TerminalScore score, UtilityTickInfo tickInfo, out TerminalCandidate candidate)
        {
            bool canExecute = score.CanExecute && IsFinite(score.ExecutionScore);
            bool canPrepare = score.CanPrepare && IsFinite(score.PreparationScore);
            candidate = default(TerminalCandidate);

            if (!canExecute && !canPrepare)
                return false;

            UtilityIntentMode mode;
            float baseScore;
            if (canExecute && canPrepare)
            {
                if (score.PreparationScore > score.ExecutionScore + Config.RepositionMargin)
                {
                    mode = UtilityIntentMode.Prepare;
                    baseScore = score.PreparationScore;
                }
                else
                {
                    mode = UtilityIntentMode.Execute;
                    baseScore = score.ExecutionScore;
                }
            }
            else if (canExecute)
            {
                mode = UtilityIntentMode.Execute;
                baseScore = score.ExecutionScore;
            }
            else
            {
                mode = UtilityIntentMode.Prepare;
                baseScore = score.PreparationScore;
            }

            if (mode == UtilityIntentMode.Prepare && IsPreparationTimedOut(index, tickInfo.TickIndex))
            {
                if (!canExecute)
                    return false;

                mode = UtilityIntentMode.Execute;
                baseScore = score.ExecutionScore;
            }

            float effectiveScore = baseScore;
            if (index == _previousNormalTerminalIndex)
                effectiveScore += Config.IntentStickiness;

            candidate = new TerminalCandidate
            {
                Index = index,
                Mode = mode,
                Score = score,
                EffectiveScore = effectiveScore
            };
            return IsFinite(effectiveScore);
        }

        private bool IsPreparationTimedOut(int terminalIndex, int tickIndex)
        {
            if (Config.PreparationTimeoutTicks <= 0)
                return false;

            if (_previousNormalTerminalIndex != terminalIndex || _previousNormalMode != UtilityIntentMode.Prepare)
                return false;

            int startTick = _prepareStartTicks[terminalIndex];
            return startTick >= 0 && tickIndex - startTick >= Config.PreparationTimeoutTicks;
        }

        private void SetNormalSelectionMemory(TerminalCandidate candidate, UtilityTickInfo tickInfo)
        {
            if (candidate.Mode == UtilityIntentMode.Prepare)
            {
                if (_previousNormalTerminalIndex != candidate.Index || _previousNormalMode != UtilityIntentMode.Prepare)
                    _prepareStartTicks[candidate.Index] = tickInfo.TickIndex;
            }
            else
            {
                _prepareStartTicks[candidate.Index] = -1;
            }

            _previousNormalTerminalIndex = candidate.Index;
            _previousNormalMode = candidate.Mode;
        }

        private void ApplySelectedSupports(UtilityTickInfo tickInfo, int supportCount)
        {
            ClearSupportActiveStates();
            ClearSelectedSupports();

            for (int i = 0; i < supportCount; i++)
            {
                int supportIndex = _builtSupports[i].Index;
                _selectedSupportIndices[i] = supportIndex;
                _selectedSupportRequired[i] = _builtSupports[i].IsRequired;

                var runtime = SupportActions[supportIndex];
                runtime.IsActive = true;
                runtime.MinHoldUntilTick = tickInfo.TickIndex + runtime.Config.MinHoldTicks;
                SupportActions[supportIndex] = runtime;
            }

            _currentSelectedSupportCount = supportCount;
        }

        private void ClearSelectedSupports()
        {
            _currentSelectedSupportCount = 0;
        }

        private void ClearSupportActiveStates()
        {
            for (int i = 0; i < SupportActions.Length; i++)
            {
                var runtime = SupportActions[i];
                runtime.IsActive = false;
                SupportActions[i] = runtime;
            }
        }

        private void MarkTerminalSelected(int terminalIndex, int tickIndex)
        {
            var runtime = TerminalActions[terminalIndex];
            runtime.LastSelectedTick = tickIndex;
            TerminalActions[terminalIndex] = runtime;
        }

        private void SetCommitted(int terminalIndex, int startTick, int lockTicks)
        {
            _committedTerminalIndex = terminalIndex;
            _commitStartTick = startTick;
            _commitUntilTick = startTick + lockTicks;
        }

        private void ClearCommitted()
        {
            _committedTerminalIndex = NoTerminal;
            _commitStartTick = -1;
            _commitUntilTick = -1;
        }

        private UtilityDecisionResult CreateDecision(UtilityIntentMode mode, int terminalIndex, int supportCount)
        {
            _currentDecision = new UtilityDecisionResult
            {
                DecisionVersion = _currentDecisionVersion,
                Mode = mode,
                SelectedTerminalIndex = terminalIndex,
                SelectedSupportCount = supportCount
            };
            _hasCurrentDecision = true;
            return _currentDecision;
        }

        private bool MatchesCurrentDecision(UtilityDecisionResult decision)
        {
            return decision.DecisionVersion == _currentDecision.DecisionVersion &&
                   decision.Mode == _currentDecision.Mode &&
                   decision.SelectedTerminalIndex == _currentDecision.SelectedTerminalIndex &&
                   decision.SelectedSupportCount == _currentDecision.SelectedSupportCount;
        }

        private static bool IsKnownMode(UtilityIntentMode mode)
        {
            switch (mode)
            {
                case UtilityIntentMode.None:
                case UtilityIntentMode.Execute:
                case UtilityIntentMode.Prepare:
                case UtilityIntentMode.Committed:
                case UtilityIntentMode.Emergency:
                    return true;
                default:
                    return false;
            }
        }

        private void ClearSingleOwnerOccupied()
        {
            for (int i = 0; i < _singleOwnerOccupied.Length; i++)
                _singleOwnerOccupied[i] = false;
        }

        private void ClearExcludedAllowedSupportBuildFailures()
        {
            for (int i = 0; i < _excludedAllowedSupportBuildFailures.Length; i++)
                _excludedAllowedSupportBuildFailures[i] = false;
        }

        private void SortBuiltSupports(int count)
        {
            Array.Sort(_builtSupports, 0, count, BuiltSupportComparer.Instance);
        }

        private bool ConflictsWithSingleOwner(int supportIndex, bool[] occupied)
        {
            var channels = SupportActions[supportIndex].Config.Channels;
            for (int i = 0; i < channels.Length; i++)
            {
                int slot = GetSingleOwnerSlot(channels[i]);
                if (slot >= 0 && occupied[slot])
                    return true;
            }

            return false;
        }

        private void OccupySingleOwnerChannels(int supportIndex, bool[] occupied)
        {
            var channels = SupportActions[supportIndex].Config.Channels;
            for (int i = 0; i < channels.Length; i++)
            {
                int slot = GetSingleOwnerSlot(channels[i]);
                if (slot >= 0)
                    occupied[slot] = true;
            }
        }

        private static int GetSingleOwnerSlot(UtilitySupportChannel channel)
        {
            switch (channel)
            {
                case UtilitySupportChannel.Movement:
                    return 0;
                case UtilitySupportChannel.Facing:
                    return 1;
                case UtilitySupportChannel.Positioning:
                    return 2;
                default:
                    return -1;
            }
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static UtilityExecutionStatus ToExecutionStatus(UtilityActionExecutionStatus status)
        {
            switch (status)
            {
                case UtilityActionExecutionStatus.Success:
                    return UtilityExecutionStatus.Success;
                case UtilityActionExecutionStatus.Failed:
                    return UtilityExecutionStatus.Failed;
                default:
                    return UtilityExecutionStatus.Rejected;
            }
        }
    }
}
