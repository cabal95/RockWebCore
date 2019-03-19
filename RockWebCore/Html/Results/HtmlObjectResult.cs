using Microsoft.AspNetCore.Mvc;

namespace RockWebCore.Html.Results
{
    public class HtmlObjectResult : ContentResult
    {
        public HtmlObjectResult( string content )
            : base()
        {
            ContentType = "text/html";
            Content = content;
            StatusCode = 200;
        }
    }
}
