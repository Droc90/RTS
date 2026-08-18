using RTS.Application.CandidateDiscovery;
using RTS.Domain.CandidateDiscovery;

namespace RTS.Application.Tests.CandidateDiscovery;

public sealed class CandidateWorkflowTests
{
    [Theory]
    [InlineData(CandidateWorkflowStatus.Proposed, CandidateWorkflowStatus.Selected, true)]
    [InlineData(CandidateWorkflowStatus.Deferred, CandidateWorkflowStatus.Selected, true)]
    [InlineData(CandidateWorkflowStatus.Rejected, CandidateWorkflowStatus.Selected, false)]
    [InlineData(CandidateWorkflowStatus.Excluded, CandidateWorkflowStatus.Selected, false)]
    [InlineData(CandidateWorkflowStatus.PreviouslyEvaluated, CandidateWorkflowStatus.Selected, false)]
    public void Transitions_enforce_the_approval_boundary(CandidateWorkflowStatus current, CandidateWorkflowStatus next, bool expected)
    {
        Assert.Equal(expected, CandidateWorkflow.CanTransition(current, next));
    }

    [Fact]
    public void Only_selected_candidates_are_approved_for_full_evaluation()
    {
        foreach (var status in Enum.GetValues<CandidateWorkflowStatus>())
            Assert.Equal(status == CandidateWorkflowStatus.Selected, CandidateWorkflow.IsApprovedForFullEvaluation(status));
    }
}
