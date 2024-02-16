namespace WordleBot.Config;

public class MessageConfig
{
    public string AlreadySubmittedFormat { get; set; } = "{0} has already submitted an answer for Wordle {1}";
    public string WinnerFormat { get; set; } = "Wordle {0} winner is {1}! Who scored {2}/6 ({3}).";
    public string TodaysAnswerFormat { get; set; } = "Today's answer was {0}";
    public string RunnersUpFormat { get; set; } = "{0} - {1}: {2} points";
    
    public string CommandUnknownDay { get; set; } = "Day {0} has not been seen yet";
    public string CommandNotAdmin { get; set; } = "{0} is not allowed to do that";
    public string RoundupEarly { get; set; } = "Cannot do another roundup now, try again later";

    public string Help { get; set; } =
        "Hambot Help:\n" +
        "{0} [day number] : display current results for a day, default today\n" +
        "{1} [day number] : immediately announces the results for the given day (admin only)\n" +
        "{2} : show player stats for the current year\n" +
        "{3} <word> : returns the date the word was the Wordle answer, or that is has not been the answer";
        
                                       
    public string CommandUnknown { get; set; } = "Unknown command";

    public IDictionary<int, IList<string>> ResultResponses { get; set; } = new Dictionary<int, IList<string>>();
}