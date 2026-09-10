using NUnit.Framework;

public class GameProgressStateTests
{
    [SetUp]
    public void SetUp()
    {
        GameProgressState.Reset();
    }

    [TearDown]
    public void TearDown()
    {
        GameProgressState.Reset();
    }

    [Test]
    public void RecordOutcome_RecordsPuzzleOutcomeAndMoralityOnlyOnce()
    {
        bool firstResult = GameProgressState.RecordOutcome("Door", "Kind", 2);
        bool repeatedResult = GameProgressState.RecordOutcome("Door", "Kind", 2);

        Assert.That(firstResult, Is.True);
        Assert.That(repeatedResult, Is.False);
        Assert.That(GameProgressState.IsPuzzleCompleted("Door"), Is.True);
        Assert.That(GameProgressState.HasOutcome("Kind"), Is.True);
        Assert.That(GameProgressState.MoralityBalance, Is.EqualTo(2));
    }

    [Test]
    public void Restore_DiscardsBlankIdsAndReplacesPreviousProgress()
    {
        GameProgressState.CompletePuzzle("OldPuzzle");

        GameProgressState.Restore(
            -1,
            new[] { "NewPuzzle", "", "NewPuzzle" },
            new[] { "Outcome", " " });

        Assert.That(GameProgressState.IsPuzzleCompleted("OldPuzzle"), Is.False);
        Assert.That(GameProgressState.IsPuzzleCompleted("NewPuzzle"), Is.True);
        Assert.That(GameProgressState.CompletedPuzzleIds.Count, Is.EqualTo(1));
        Assert.That(GameProgressState.HasOutcome("Outcome"), Is.True);
        Assert.That(GameProgressState.RecordedOutcomeIds.Count, Is.EqualTo(1));
        Assert.That(GameProgressState.MoralityBalance, Is.EqualTo(-1));
    }
}
