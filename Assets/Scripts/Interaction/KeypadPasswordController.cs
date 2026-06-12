public class KeypadPasswordController
{
    public enum SubmissionResult
    {
        Incomplete,
        Accepted,
        Rejected
    }

    private readonly string correctPassword;

    public string EnteredDigits { get; private set; } = string.Empty;
    public bool IsAccepted { get; private set; }

    public KeypadPasswordController(string password)
    {
        correctPassword = password;
    }

    public SubmissionResult SubmitDigit(int digit)
    {
        if (IsAccepted || digit < 0 || digit > 9 || EnteredDigits.Length >= correctPassword.Length)
        {
            return SubmissionResult.Incomplete;
        }

        EnteredDigits += digit.ToString();
        if (EnteredDigits.Length < correctPassword.Length)
        {
            return SubmissionResult.Incomplete;
        }

        if (EnteredDigits == correctPassword)
        {
            IsAccepted = true;
            return SubmissionResult.Accepted;
        }

        EnteredDigits = string.Empty;
        return SubmissionResult.Rejected;
    }

    public void RemoveLastDigit()
    {
        if (IsAccepted || EnteredDigits.Length == 0)
        {
            return;
        }

        EnteredDigits = EnteredDigits.Substring(0, EnteredDigits.Length - 1);
    }

    public void Reset()
    {
        EnteredDigits = string.Empty;
        IsAccepted = false;
    }
}
