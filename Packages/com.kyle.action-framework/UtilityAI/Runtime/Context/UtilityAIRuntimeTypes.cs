using System;
using System.Collections.Generic;

namespace UtilityAI
{
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
}
