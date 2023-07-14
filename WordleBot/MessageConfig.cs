namespace WordleBot;

public class MessageConfig
{
    public string AlreadySubmitted { get; set; } = "{} has already submitted an answer for Wordle {}";
    public string SubmittedTooLate { get; set; } = "{} is too late for Wordle {}";
}