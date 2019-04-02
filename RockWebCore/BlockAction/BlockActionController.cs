using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Rock;
using Rock.Web.Cache;
using RockWebCore.UI;

namespace RockWebCore.BlockAction
{
    [ApiController]
    [Route( "api/[controller]" )]
    public class BlockActionController : ControllerBase
    {
        [Route( "{blockId}/{actionName}" )]
        [HttpGet]
        public async Task<IActionResult> Get( int blockId, string actionName )
        {
            try
            {
                var block = BlockCache.Get( blockId ).GetMappedBlockType( HttpContext.RequestServices );

                if ( !( block is IAsyncActionBlock actionBlock ) )
                {
                    return new BadRequestObjectResult( "Block does not support actions." );
                }

                ActionData actionData = new ActionData();

                foreach ( var q in Request.Query )
                {
                    actionData.Parameters.AddOrReplace( q.Key, JToken.FromObject( q.Value.ToString() ) );
                }

                return await actionBlock.ProcessActionAsync( actionName, actionData );
            }
            catch ( Exception ex )
            {
                return BadRequest( ex.Message );
            }
        }

        [Route( "{blockId}/{actionName}")]
        public async Task<IActionResult> Postback( int blockId, string actionName, [FromBody] JToken parameters, [FromServices] RockRequestContext rockRequestContext )
        {
            try
            {
                var blockCache = BlockCache.Get( blockId );
                var block = blockCache.GetMappedBlockType( HttpContext.RequestServices );

                block.Block = blockCache;
                block.PageCache = blockCache.Page;
                block.RockPage = new RockPage( PageCache.Get( blockCache.PageId.Value ), rockRequestContext );
                rockRequestContext.CurrentPage = block.RockPage;

                if ( !( block is IAsyncActionBlock actionBlock ) )
                {
                    return new BadRequestObjectResult( "Block does not support actions." );
                }

                ActionData actionData;
                try
                {
                    actionData = parameters.ToObject<ActionData>();
                }
                catch
                {
                    return new BadRequestObjectResult( "Invalid parameter data." );
                }

                foreach ( var q in Request.Query )
                {
                    actionData.Parameters.AddOrReplace( q.Key, JToken.FromObject( q.Value.ToString() ) );
                }

                return await actionBlock.ProcessActionAsync( actionName, actionData );
            }
            catch ( Exception ex )
            {
                return BadRequest( ex.Message );
            }
        }
    }
}
