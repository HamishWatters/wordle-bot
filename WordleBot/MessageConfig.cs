namespace WordleBot;

public class MessageConfig
{
    public string AlreadySubmittedFormat { get; set; } = "{} has already submitted an answer for Wordle {}";
    public string SubmittedTooLateFormat { get; set; } = "{} is too late for Wordle {}";
    public string WinnerFormat { get; set; } = "Wordle {0} winner is {1}! Who scored {2}/6 ({3}).";
    public string TodaysAnswerFormat { get; set; } = "Today's answer was {}";
    public string RunnersUpFormat { get; set; } = "{0} - {1}: {2} points";
}