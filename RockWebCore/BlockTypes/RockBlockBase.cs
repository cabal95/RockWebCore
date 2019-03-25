using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using Rock.Model;
using Rock.Web.Cache;

using RockWebCore.BlockAction;

namespace RockWebCore.BlockTypes
{
    public class RockBlockBase : IBlockType, IAsyncActionBlock
    {
        #region Properties

        public int BlockId => Block.Id;

        public IRockPage RockPage { get; set; }

        public PageCache PageCache { get; set; }

        public BlockCache Block { get; set; }

        public Person CurrentPerson => RockPage.CurrentPerson;

        #endregion

        #region Render Methods

        public virtual Task PreRenderAsync()
        {
            return Task.CompletedTask;
        }

        public virtual Task RenderAsync( TextWriter writer )
        {
            return Task.CompletedTask;
        }

        #endregion

        #region Block Support Methods

        /// <summary>
        /// Gets the attribute value.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public virtual string GetAttributeValue( string key )
        {
            return Block.GetAttributeValue( key );
        }

        /// <summary>
        /// Determines whether the user is authorized for the specified action.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <returns>
        ///   <c>true</c> if the user is authorized for the specified action; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool IsUserAuthorized( string action )
        {
            return Block.IsAuthorized( action, CurrentPerson );
        }

        /// <summary>
        /// Resolves the rock URL.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns></returns>
        public string ResolveRockUrl( string url )
        {
            return RockPage.ResolveRockUrl( url );
        }

        /// <summary>
        /// Checks various page parameters and returns the match.
        /// </summary>
        /// <param name="name">The parameter name whose value is wanted.</param>
        /// <returns></returns>
        public string PageParameter( string name )
        {
            return RockPage.Context.PageParameter( name );
        }

        #endregion

        #region IAsyncActionBlock Methods

        /// <summary>
        /// Processes the specified block action.
        /// </summary>
        /// <param name="block">The block.</param>
        /// <param name="actionName">Name of the action.</param>
        /// <param name="actionData">The action data.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">
        /// actionName
        /// or
        /// actionData
        /// </exception>
        public static async Task<IActionResult> ProcessActionAsync( IAsyncActionBlock block, string actionName, ActionData actionData )
        {
            MethodInfo action;

            if ( actionName == null )
            {
                throw new ArgumentNullException( nameof( actionName ) );
            }

            if ( actionData == null )
            {
                throw new ArgumentNullException( nameof( actionData ) );
            }

            //
            // Find the action they requested.
            //
            action = block.GetType().GetMethods( BindingFlags.Instance | BindingFlags.Public )
                .SingleOrDefault( m => m.GetCustomAttributes( true ).Any( a => typeof( ActionNameAttribute ).IsAssignableFrom( a.GetType() ) && ( ( ActionNameAttribute ) a ).Name == actionName ) );

            if ( action == null )
            {
                return new NotFoundObjectResult( "Action was not found." );
            }

            //
            // Verify the action return type is either void, IActionResult or Task<IActionResult>.
            //
            if ( action.ReturnType != typeof( void ) && action.ReturnType != typeof( IActionResult ) && action.ReturnType != typeof( Task ) && action.ReturnType != typeof( Task<IActionResult> ) )
            {
                return new BadRequestObjectResult( "Invalid return type for action." );
            }

            var methodParameters = action.GetParameters();
            var actionParameters = new List<object>();

            //
            // Go through each parameter and convert it to the proper type.
            //
            for ( int i = 0; i < methodParameters.Length; i++ )
            {
                var key = actionData.Parameters.Keys.SingleOrDefault( k => k.ToLowerInvariant() == methodParameters[i].Name.ToLower() );

                if ( key != null )
                {
                    try
                    {
                        actionParameters.Add( actionData.Parameters[key].ToObject( methodParameters[i].ParameterType ) );
                    }
                    catch
                    {
                        return new BadRequestObjectResult( $"Parameter type mismatch for '{methodParameters[i].Name}'." );
                    }
                }
                else if ( methodParameters[i].IsOptional )
                {
                    actionParameters.Add( Type.Missing );
                }
                else
                {
                    return new BadRequestObjectResult( $"Parameter '{methodParameters[i].Name}' is required." );
                }
            }

            var result = action.Invoke( block, actionParameters.ToArray() );

            //
            // Handle the result type.
            //
            if ( action.ReturnType == typeof( Task<IActionResult> ) )
            {
                return await ( Task<IActionResult> ) result;
            }
            else if ( action.ReturnType == typeof( Task ) )
            {
                await ( Task ) result;

                return new OkResult();
            }
            else if ( action.ReturnType == typeof( IActionResult ) )
            {
                return ( IActionResult ) result;
            }
            else
            {
                /* Void */
                return new OkResult();
            }
        }

        /// <summary>
        /// Processes the block action.
        /// </summary>
        /// <param name="actionName">Name of the action.</param>
        /// <param name="actionData">The action data.</param>
        /// <returns></returns>
        public async Task<IActionResult> ProcessActionAsync( string actionName, ActionData actionData )
        {
            return await ProcessActionAsync( this, actionName, actionData );
        }

        #endregion
    }
}
