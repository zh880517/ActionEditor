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
    /// Terminal 候选的临时排序数据。
    /// </summary>
    internal struct UtilityAITerminalCandidate
    {
        public int Index;
        public UtilityIntentMode Mode;
        public TerminalScore Score;
        public float EffectiveScore;
    }

    /// <summary>
    /// Support 候选的临时排序数据。
    /// </summary>
    internal struct UtilityAISupportCandidate
    {
        public int Index;
        public bool IsRequired;
        public bool HasMinHold;
        public SupportScore Score;
    }

    /// <summary>
    /// 已选 Support 的构建顺序数据。
    /// </summary>
    internal struct UtilityAIBuiltSupport
    {
        public int Index;
        public bool IsRequired;
        public SupportScore Score;
    }

    /// <summary>
    /// 单次决策过程中复用的候选和选择缓冲区。
    /// </summary>
    internal struct UtilityAISelectionBuffer
    {
        public readonly int[] SelectedSupportIndices;
        public readonly bool[] SelectedSupportRequired;
        public readonly List<UtilityAITerminalCandidate> TerminalCandidates;
        public readonly List<UtilityAISupportCandidate> SupportCandidates;
        public readonly List<UtilityAISupportCandidate> AllowedSupportCandidates;
        public readonly UtilityAIBuiltSupport[] BuiltSupports;
        public readonly bool[] SingleOwnerOccupied;
        public readonly bool[] ExcludedAllowedSupportBuildFailures;

        public UtilityAISelectionBuffer(int terminalCount, int supportCount)
        {
            SelectedSupportIndices = new int[Math.Max(1, supportCount)];
            SelectedSupportRequired = new bool[Math.Max(1, supportCount)];
            TerminalCandidates = new List<UtilityAITerminalCandidate>(terminalCount);
            SupportCandidates = new List<UtilityAISupportCandidate>(supportCount);
            AllowedSupportCandidates = new List<UtilityAISupportCandidate>(supportCount);
            BuiltSupports = new UtilityAIBuiltSupport[Math.Max(1, supportCount)];
            SingleOwnerOccupied = new bool[3];
            ExcludedAllowedSupportBuildFailures = new bool[Math.Max(1, supportCount)];
        }
    }

    /// <summary>
    /// Evaluate 与 Execute 之间共享的当前决策状态。
    /// </summary>
    internal struct UtilityAIDecisionState
    {
        public int CurrentDecisionVersion;
        public int LastExecutedDecisionVersion;
        public int CurrentSelectedSupportCount;
        public UtilityDecisionResult CurrentDecision;
        public bool HasCurrentDecision;

        public static UtilityAIDecisionState Create()
        {
            return new UtilityAIDecisionState
            {
                LastExecutedDecisionVersion = -1
            };
        }
    }

    /// <summary>
    /// Terminal 提交锁定状态。
    /// </summary>
    internal struct UtilityAICommitState
    {
        public int TerminalIndex;
        public int StartTick;
        public int UntilTick;

        public static UtilityAICommitState CreateEmpty()
        {
            return new UtilityAICommitState
            {
                TerminalIndex = -1,
                StartTick = -1,
                UntilTick = -1
            };
        }
    }

    /// <summary>
    /// 普通 Terminal 选择的历史状态，用于粘性分和准备超时。
    /// </summary>
    internal struct UtilityAINormalSelectionState
    {
        public readonly int[] PrepareStartTicks;
        public int PreviousTerminalIndex;
        public UtilityIntentMode PreviousMode;

        public UtilityAINormalSelectionState(int terminalCount)
        {
            PrepareStartTicks = new int[terminalCount];
            PreviousTerminalIndex = -1;
            PreviousMode = UtilityIntentMode.None;

            for (int i = 0; i < PrepareStartTicks.Length; i++)
                PrepareStartTicks[i] = -1;
        }
    }

    /// <summary>
    /// UtilityAI 决策运行时，驱动 Evaluate 和 Execute 两阶段流程。
    /// </summary>
    public sealed class UtilityAIRuntime<TContext>
        where TContext : IUtilityIntentState, IUtilitySupportConstraintProvider
    {
        private sealed class TerminalCandidateComparer : IComparer<UtilityAITerminalCandidate>
        {
            public static readonly TerminalCandidateComparer Instance = new TerminalCandidateComparer();

            public int Compare(UtilityAITerminalCandidate left, UtilityAITerminalCandidate right)
            {
                int scoreCompare = right.EffectiveScore.CompareTo(left.EffectiveScore);
                if (scoreCompare != 0)
                    return scoreCompare;
                return left.Index.CompareTo(right.Index);
            }
        }

        private sealed class SupportCandidateComparer : IComparer<UtilityAISupportCandidate>
        {
            public static readonly SupportCandidateComparer Instance = new SupportCandidateComparer();

            public int Compare(UtilityAISupportCandidate left, UtilityAISupportCandidate right)
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

        private sealed class BuiltSupportComparer : IComparer<UtilityAIBuiltSupport>
        {
            public static readonly BuiltSupportComparer Instance = new BuiltSupportComparer();

            public int Compare(UtilityAIBuiltSupport left, UtilityAIBuiltSupport right)
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

        private readonly UtilityAISelectionBuffer _selection;
        private UtilityAIDecisionState _decision;
        private UtilityAICommitState _commit;
        private UtilityAINormalSelectionState _normalSelection;

        internal UtilityAIRuntime(
            UtilityAIConfig config,
            TerminalActionRuntime<TContext>[] terminalActions,
            SupportActionRuntime<TContext>[] supportActions)
        {
            Config = config;
            TerminalActions = terminalActions;
            SupportActions = supportActions;
            _selection = new UtilityAISelectionBuffer(terminalActions.Length, supportActions.Length);
            _decision = UtilityAIDecisionState.Create();
            _commit = UtilityAICommitState.CreateEmpty();
            _normalSelection = new UtilityAINormalSelectionState(terminalActions.Length);
        }

        public UtilityDecisionResult Evaluate(TContext context, UtilityTickInfo tickInfo)
        {
            _decision.CurrentDecisionVersion++;
            ClearSelectedSupports();
            context.ResetUtilityIntentState();

            if (_commit.TerminalIndex != NoTerminal)
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
            _normalSelection.PreviousTerminalIndex = NoTerminal;
            _normalSelection.PreviousMode = UtilityIntentMode.None;
            return CreateDecision(UtilityIntentMode.None, NoTerminal, 0);
        }

        public UtilityExecutionResult Execute(TContext context, UtilityDecisionResult decision, UtilityTickInfo tickInfo)
        {
            if (decision.DecisionVersion != _decision.CurrentDecisionVersion ||
                !_decision.HasCurrentDecision ||
                decision.DecisionVersion == _decision.LastExecutedDecisionVersion)
                return new UtilityExecutionResult { Status = UtilityExecutionStatus.Rejected };

            if (!IsKnownMode(decision.Mode) || !MatchesCurrentDecision(decision))
                return RejectCurrentDecision(decision);

            if (decision.Mode == UtilityIntentMode.None)
            {
                _decision.LastExecutedDecisionVersion = decision.DecisionVersion;
                return new UtilityExecutionResult { Status = UtilityExecutionStatus.Success };
            }

            if (decision.SelectedTerminalIndex < 0 || decision.SelectedTerminalIndex >= TerminalActions.Length)
                return RejectCurrentDecision(decision);

            if (decision.SelectedSupportCount != _decision.CurrentSelectedSupportCount)
                return RejectCurrentDecision(decision);

            if (decision.Mode == UtilityIntentMode.Execute || decision.Mode == UtilityIntentMode.Emergency)
            {
                if (decision.Mode == UtilityIntentMode.Emergency)
                    ClearCommitted();

                var terminalStatus = ExecuteTerminal(context, decision, tickInfo);
                if (terminalStatus != UtilityActionExecutionStatus.Success)
                {
                    ClearSupportActiveStates();
                    _decision.LastExecutedDecisionVersion = decision.DecisionVersion;
                    return new UtilityExecutionResult { Status = ToExecutionStatus(terminalStatus) };
                }

                var terminalRuntime = TerminalActions[decision.SelectedTerminalIndex];
                if (terminalRuntime.LastScore.LockTicks > 0)
                    SetCommitted(decision.SelectedTerminalIndex, tickInfo.TickIndex, terminalRuntime.LastScore.LockTicks);
                else if (decision.Mode == UtilityIntentMode.Emergency)
                    ClearCommitted();
            }

            var supportStatus = ExecuteSupports(context, decision, tickInfo);
            _decision.LastExecutedDecisionVersion = decision.DecisionVersion;
            return new UtilityExecutionResult { Status = supportStatus };
        }

        public int GetSelectedSupportIndex(int slot)
        {
            if (slot < 0 || slot >= _decision.CurrentSelectedSupportCount)
                return -1;
            return _selection.SelectedSupportIndices[slot];
        }

        private UtilityExecutionResult RejectCurrentDecision(UtilityDecisionResult decision)
        {
            _decision.LastExecutedDecisionVersion = decision.DecisionVersion;
            return new UtilityExecutionResult { Status = UtilityExecutionStatus.Rejected };
        }

        private bool TrySelectNormal(TContext context, UtilityTickInfo tickInfo, out UtilityDecisionResult decision)
        {
            var candidates = _selection.TerminalCandidates;
            candidates.Clear();
            for (int i = 0; i < TerminalActions.Length; i++)
            {
                var runtime = TerminalActions[i];
                var score = runtime.Handler.Score(context, runtime.Config, new TerminalScoreInput
                {
                    TickInfo = tickInfo,
                    TerminalIndex = i,
                    DecisionVersion = _decision.CurrentDecisionVersion,
                    HasCommittedTerminal = false,
                    CommittedTerminalIndex = NoTerminal
                });

                runtime.LastScore = score;
                TerminalActions[i] = runtime;

                UtilityAITerminalCandidate candidate;
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
            var candidates = _selection.TerminalCandidates;
            candidates.Clear();
            for (int i = 0; i < TerminalActions.Length; i++)
            {
                if (i == _commit.TerminalIndex)
                    continue;

                var runtime = TerminalActions[i];
                bool hasCommittedTerminal = _commit.TerminalIndex != NoTerminal;
                var score = runtime.Handler.Score(context, runtime.Config, new TerminalScoreInput
                {
                    TickInfo = tickInfo,
                    TerminalIndex = i,
                    DecisionVersion = _decision.CurrentDecisionVersion,
                    HasCommittedTerminal = hasCommittedTerminal,
                    CommittedTerminalIndex = _commit.TerminalIndex
                });

                runtime.LastScore = score;
                TerminalActions[i] = runtime;

                if (!score.IsEmergency || !IsFinite(score.EmergencyScore))
                    continue;

                candidates.Add(new UtilityAITerminalCandidate
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
            var runtime = TerminalActions[_commit.TerminalIndex];
            context.ResetUtilityIntentState();
            var buildStatus = runtime.Handler.BuildIntent(context, runtime.Config, new TerminalIntentBuildInput
            {
                TickInfo = tickInfo,
                TerminalIndex = _commit.TerminalIndex,
                DecisionVersion = _decision.CurrentDecisionVersion,
                Mode = UtilityIntentMode.Committed,
                Score = runtime.LastScore
            });

            if (buildStatus != UtilityIntentBuildStatus.Success)
            {
                decision = default(UtilityDecisionResult);
                return false;
            }

            int builtCount;
            if (!TryBuildSupports(context, tickInfo, _commit.TerminalIndex, UtilityIntentMode.Committed, out builtCount))
            {
                decision = default(UtilityDecisionResult);
                return false;
            }

            ApplySelectedSupports(tickInfo, builtCount);
            MarkTerminalSelected(_commit.TerminalIndex, tickInfo.TickIndex);
            decision = CreateDecision(UtilityIntentMode.Committed, _commit.TerminalIndex, builtCount);
            return true;
        }

        private bool TryBuildTerminalCandidate(
            TContext context,
            UtilityTickInfo tickInfo,
            UtilityAITerminalCandidate candidate,
            out UtilityDecisionResult decision)
        {
            context.ResetUtilityIntentState();

            var runtime = TerminalActions[candidate.Index];
            var buildStatus = runtime.Handler.BuildIntent(context, runtime.Config, new TerminalIntentBuildInput
            {
                TickInfo = tickInfo,
                TerminalIndex = candidate.Index,
                DecisionVersion = _decision.CurrentDecisionVersion,
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

            var candidates = _selection.SupportCandidates;
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
                    DecisionVersion = _decision.CurrentDecisionVersion,
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

                candidates.Add(new UtilityAISupportCandidate
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

                _selection.ExcludedAllowedSupportBuildFailures[failedAllowedSupportIndex] = true;
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
            List<UtilityAISupportCandidate> candidates,
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

                if (ConflictsWithSingleOwner(candidates[i].Index, _selection.SingleOwnerOccupied))
                    return false;

                OccupySingleOwnerChannels(candidates[i].Index, _selection.SingleOwnerOccupied);
                _selection.BuiltSupports[selectedCount++] = new UtilityAIBuiltSupport
                {
                    Index = candidates[i].Index,
                    IsRequired = true,
                    Score = candidates[i].Score
                };
            }

            var allowedCandidates = _selection.AllowedSupportCandidates;
            allowedCandidates.Clear();
            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i].IsRequired)
                    continue;

                if (_selection.ExcludedAllowedSupportBuildFailures[candidates[i].Index])
                    continue;

                allowedCandidates.Add(candidates[i]);
            }

            allowedCandidates.Sort(SupportCandidateComparer.Instance);
            for (int i = 0; i < allowedCandidates.Count; i++)
            {
                var candidate = allowedCandidates[i];

                if (selectedCount >= Config.MaxSupportCount)
                    break;

                if (ConflictsWithSingleOwner(candidate.Index, _selection.SingleOwnerOccupied))
                    continue;

                OccupySingleOwnerChannels(candidate.Index, _selection.SingleOwnerOccupied);
                _selection.BuiltSupports[selectedCount++] = new UtilityAIBuiltSupport
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
                var support = _selection.BuiltSupports[i];
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
            UtilityAIBuiltSupport support)
        {
            var runtime = SupportActions[support.Index];
            var status = runtime.Handler.BuildIntent(context, runtime.Config, new SupportIntentBuildInput
            {
                TickInfo = tickInfo,
                SupportIndex = support.Index,
                SelectedTerminalIndex = terminalIndex,
                DecisionVersion = _decision.CurrentDecisionVersion,
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
                DecisionVersion = _decision.CurrentDecisionVersion,
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
                int supportIndex = _selection.SelectedSupportIndices[i];
                var runtime = SupportActions[supportIndex];
                var status = runtime.Handler.Execute(context, runtime.Config, new SupportExecuteInput
                {
                    TickInfo = tickInfo,
                    SupportIndex = supportIndex,
                    SelectedTerminalIndex = decision.SelectedTerminalIndex,
                    DecisionVersion = decision.DecisionVersion,
                    Mode = decision.Mode,
                    IsRequired = _selection.SelectedSupportRequired[i],
                    Score = runtime.LastScore
                });

                if (status != UtilityActionExecutionStatus.Success)
                    hasFailure = true;
            }

            return hasFailure ? UtilityExecutionStatus.PartialFailed : UtilityExecutionStatus.Success;
        }

        private bool IsCommittedFinished(TContext context, UtilityTickInfo tickInfo)
        {
            var runtime = TerminalActions[_commit.TerminalIndex];
            return runtime.Handler.IsCommitFinished(context, runtime.Config, new TerminalCommitInput
            {
                TickInfo = tickInfo,
                TerminalIndex = _commit.TerminalIndex,
                DecisionVersion = _decision.CurrentDecisionVersion,
                CommitStartTick = _commit.StartTick,
                CommitUntilTick = _commit.UntilTick,
                Score = runtime.LastScore
            });
        }

        private bool CanCommittedBeInterrupted(TContext context, UtilityTickInfo tickInfo)
        {
            var runtime = TerminalActions[_commit.TerminalIndex];
            return runtime.Handler.CanBeInterrupted(context, runtime.Config, new TerminalInterruptInput
            {
                TickInfo = tickInfo,
                TerminalIndex = _commit.TerminalIndex,
                DecisionVersion = _decision.CurrentDecisionVersion,
                CommitStartTick = _commit.StartTick,
                CommitUntilTick = _commit.UntilTick,
                Score = runtime.LastScore
            });
        }

        private bool TryCreateNormalCandidate(int index, TerminalScore score, UtilityTickInfo tickInfo, out UtilityAITerminalCandidate candidate)
        {
            bool canExecute = score.CanExecute && IsFinite(score.ExecutionScore);
            bool canPrepare = score.CanPrepare && IsFinite(score.PreparationScore);
            candidate = default(UtilityAITerminalCandidate);

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
            if (index == _normalSelection.PreviousTerminalIndex)
                effectiveScore += Config.IntentStickiness;

            candidate = new UtilityAITerminalCandidate
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

            if (_normalSelection.PreviousTerminalIndex != terminalIndex || _normalSelection.PreviousMode != UtilityIntentMode.Prepare)
                return false;

            int startTick = _normalSelection.PrepareStartTicks[terminalIndex];
            return startTick >= 0 && tickIndex - startTick >= Config.PreparationTimeoutTicks;
        }

        private void SetNormalSelectionMemory(UtilityAITerminalCandidate candidate, UtilityTickInfo tickInfo)
        {
            if (candidate.Mode == UtilityIntentMode.Prepare)
            {
                if (_normalSelection.PreviousTerminalIndex != candidate.Index || _normalSelection.PreviousMode != UtilityIntentMode.Prepare)
                    _normalSelection.PrepareStartTicks[candidate.Index] = tickInfo.TickIndex;
            }
            else
            {
                _normalSelection.PrepareStartTicks[candidate.Index] = -1;
            }

            _normalSelection.PreviousTerminalIndex = candidate.Index;
            _normalSelection.PreviousMode = candidate.Mode;
        }

        private void ApplySelectedSupports(UtilityTickInfo tickInfo, int supportCount)
        {
            ClearSupportActiveStates();
            ClearSelectedSupports();

            for (int i = 0; i < supportCount; i++)
            {
                int supportIndex = _selection.BuiltSupports[i].Index;
                _selection.SelectedSupportIndices[i] = supportIndex;
                _selection.SelectedSupportRequired[i] = _selection.BuiltSupports[i].IsRequired;

                var runtime = SupportActions[supportIndex];
                runtime.IsActive = true;
                runtime.MinHoldUntilTick = tickInfo.TickIndex + runtime.Config.MinHoldTicks;
                SupportActions[supportIndex] = runtime;
            }

            _decision.CurrentSelectedSupportCount = supportCount;
        }

        private void ClearSelectedSupports()
        {
            _decision.CurrentSelectedSupportCount = 0;
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
            _commit.TerminalIndex = terminalIndex;
            _commit.StartTick = startTick;
            _commit.UntilTick = startTick + lockTicks;
        }

        private void ClearCommitted()
        {
            _commit.TerminalIndex = NoTerminal;
            _commit.StartTick = -1;
            _commit.UntilTick = -1;
        }

        private UtilityDecisionResult CreateDecision(UtilityIntentMode mode, int terminalIndex, int supportCount)
        {
            _decision.CurrentDecision = new UtilityDecisionResult
            {
                DecisionVersion = _decision.CurrentDecisionVersion,
                Mode = mode,
                SelectedTerminalIndex = terminalIndex,
                SelectedSupportCount = supportCount
            };
            _decision.HasCurrentDecision = true;
            return _decision.CurrentDecision;
        }

        private bool MatchesCurrentDecision(UtilityDecisionResult decision)
        {
            return decision.DecisionVersion == _decision.CurrentDecision.DecisionVersion &&
                   decision.Mode == _decision.CurrentDecision.Mode &&
                   decision.SelectedTerminalIndex == _decision.CurrentDecision.SelectedTerminalIndex &&
                   decision.SelectedSupportCount == _decision.CurrentDecision.SelectedSupportCount;
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
            for (int i = 0; i < _selection.SingleOwnerOccupied.Length; i++)
                _selection.SingleOwnerOccupied[i] = false;
        }

        private void ClearExcludedAllowedSupportBuildFailures()
        {
            for (int i = 0; i < _selection.ExcludedAllowedSupportBuildFailures.Length; i++)
                _selection.ExcludedAllowedSupportBuildFailures[i] = false;
        }

        private void SortBuiltSupports(int count)
        {
            Array.Sort(_selection.BuiltSupports, 0, count, BuiltSupportComparer.Instance);
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
