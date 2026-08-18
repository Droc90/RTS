using RTS.Domain.CandidateDiscovery;

namespace RTS.Application.CandidateDiscovery;

public static class CandidateWorkflow
{
    public static bool CanTransition(CandidateWorkflowStatus current, CandidateWorkflowStatus next) =>
        current == next || current switch
        {
            CandidateWorkflowStatus.Proposed => next is CandidateWorkflowStatus.Selected or CandidateWorkflowStatus.Rejected or CandidateWorkflowStatus.Deferred or CandidateWorkflowStatus.Excluded,
            CandidateWorkflowStatus.Deferred => next is CandidateWorkflowStatus.Proposed or CandidateWorkflowStatus.Selected or CandidateWorkflowStatus.Rejected or CandidateWorkflowStatus.Excluded,
            CandidateWorkflowStatus.Selected => next is CandidateWorkflowStatus.Rejected or CandidateWorkflowStatus.Deferred,
            _ => false
        };

    public static bool IsApprovedForFullEvaluation(CandidateWorkflowStatus status) =>
        status == CandidateWorkflowStatus.Selected;
}
