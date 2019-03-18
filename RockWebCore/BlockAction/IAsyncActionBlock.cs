using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

namespace RockWebCore.BlockAction
{
    public interface IAsyncActionBlock
    {
        Task<IActionResult> ProcessActionAsync( string actionName, ActionData actionData );
    }
}
