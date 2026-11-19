namespace Fluenty.Models;

public class ErrorViewModel
{
    public string RequestId { get; set; }

    public bool ShowRequestId()
    {
        return RequestId != null && RequestId != "";
    }
}
