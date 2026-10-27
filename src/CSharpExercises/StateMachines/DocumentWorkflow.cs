namespace CSharpExercises.StateMachines;

public enum DocumentState { Draft, InReview, Approved, Published, Archived }

public enum DocumentAction { Submit, Approve, RequestChanges, Publish, Archive, Restore }

/// <summary>
/// A publishing workflow on top of <see cref="StateMachine{TState, TTrigger}"/>. The rules live in one place,
/// in the constructor, and read almost like the requirements:
/// <list type="bullet">
/// <item>a draft needs a title before it can be submitted;</item>
/// <item>it needs two approvals from different reviewers (not the author) to leave review;</item>
/// <item>requesting changes sends it back to draft and clears the approvals.</item>
/// </list>
/// </summary>
public sealed class DocumentWorkflow
{
    public const int RequiredApprovals = 2;

    private readonly StateMachine<DocumentState, DocumentAction> _machine = new(DocumentState.Draft);
    private readonly HashSet<string> _approvedBy = new(StringComparer.OrdinalIgnoreCase);

    public DocumentWorkflow(string author)
    {
        Author = author;

        _machine.Configure(DocumentState.Draft)
                .PermitIf(DocumentAction.Submit, DocumentState.InReview, () => !string.IsNullOrWhiteSpace(Title), "the document needs a title")
            .Configure(DocumentState.InReview)
                .OnEntry(_approvedBy.Clear)
                // Approve while still short of the quorum: stay in review (a transition to the same state).
                .PermitIf(DocumentAction.Approve, DocumentState.InReview, () => _approvedBy.Count < RequiredApprovals - 1, "")
                .PermitIf(DocumentAction.Approve, DocumentState.Approved, () => _approvedBy.Count == RequiredApprovals - 1, "")
                .Permit(DocumentAction.RequestChanges, DocumentState.Draft)
            .Configure(DocumentState.Approved)
                .Permit(DocumentAction.Publish, DocumentState.Published)
                .Permit(DocumentAction.RequestChanges, DocumentState.Draft)
            .Configure(DocumentState.Published)
                .OnEntry(() => PublishedVersion++)
                .Permit(DocumentAction.Archive, DocumentState.Archived)
            .Configure(DocumentState.Archived)
                .Permit(DocumentAction.Restore, DocumentState.Draft);
    }

    public string Author { get; }
    public string? Title { get; set; }
    public int PublishedVersion { get; private set; }

    public DocumentState State => _machine.State;
    public IReadOnlyCollection<string> ApprovedBy => _approvedBy;
    public IReadOnlyList<DocumentAction> AvailableActions => _machine.PermittedTriggers;
    public int TransitionCount => _machine.History.Count;

    public void Submit() => _machine.Fire(DocumentAction.Submit);

    public void Approve(string reviewer)
    {
        // Rules about *who* acts are checked here; the machine only knows about states and counts.
        if (string.Equals(reviewer, Author, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Authors cannot approve their own document.");
        if (State == DocumentState.InReview && _approvedBy.Contains(reviewer))
            throw new InvalidOperationException($"{reviewer} has already approved this document.");

        _machine.Fire(DocumentAction.Approve);
        if (State is DocumentState.InReview or DocumentState.Approved) _approvedBy.Add(reviewer);
    }

    public void RequestChanges() => _machine.Fire(DocumentAction.RequestChanges);
    public void Publish() => _machine.Fire(DocumentAction.Publish);
    public void Archive() => _machine.Fire(DocumentAction.Archive);
    public void Restore() => _machine.Fire(DocumentAction.Restore);
}
