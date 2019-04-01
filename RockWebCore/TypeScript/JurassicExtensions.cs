using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Jurassic;
using Jurassic.Library;

namespace Rock.TypeScript
{
    public static class JurassicExtensions
    {
        /// <summary>
        /// Gets the property value cast to the specified type.
        /// </summary>
        /// <returns>The property value.</returns>
        /// <param name="source">The source object whose property is to be retrieved.</param>
        /// <param name="key">The key name of th eproperty.</param>
        /// <typeparam name="T">The type of value to return.</typeparam>
        public static T GetPropertyValue<T>( this ObjectInstance source, string key )
        {
            return ( T ) source.GetPropertyValue( key );
        }

        /// <summary>
        /// Cast the JavaScript object to a CLR type.
        /// </summary>
        /// <returns>A new <typeparamref name="T"/> that contains the properties from the JavaScript object.</returns>
        /// <param name="source">The JavaScript object to be cast.</param>
        /// <typeparam name="T">The CLR object to cast the JavaScript object into.</typeparam>
        public static T Cast<T>( this ObjectInstance source ) where T : new()
        {
            return ( T ) CastInternal( source, typeof( T ) );
        }

        /// <summary>
        /// Cast the JavaScript array to a collection of CLR types.
        /// </summary>
        /// <returns>A new collection ot <typeparamref name="T"/> objects.</returns>
        /// <param name="source">The JavaScript array to cast.</param>
        /// <typeparam name="T">The CLR object that makes up the individual elements.</typeparam>
        public static ICollection<T> Cast<T>( this ArrayInstance source ) where T : new()
        {
            return ( ICollection<T> ) CastInternal( source, typeof( T ) );
        }

        /// <summary>
        /// Gets the default value for the type.
        /// </summary>
        /// <returns>The default value of the specified type.</returns>
        /// <typeparam name="T">The type whose default value is needed.</typeparam>
        private static T GetDefaultGeneric<T>()
        {
            return default( T );
        }

        /// <summary>
        /// Casts the JavaScript value to the target type.
        /// </summary>
        /// <returns>The CLR value.</returns>
        /// <param name="source">The JavaScript value to be cast.</param>
        /// <param name="type">The target CLR type.</param>
        private static object CastInternal( object source, Type type )
        {
            if ( source is Undefined )
            {
                return typeof( JurassicExtensions ).GetMethod( "GetDefaultGeneric", BindingFlags.Static | BindingFlags.NonPublic ).MakeGenericMethod( type ).Invoke( null, null );
            }
            else if ( type == typeof( string ) )
            {
                return source?.ToString();
            }
            else if ( type == typeof( bool ) )
            {
                return Convert.ToBoolean( source );
            }
            else if ( type == typeof( int ) )
            {
                return Convert.ToInt32( source );
            }
            else if ( type == typeof( double ) )
            {
                return Convert.ToDouble( source );
            }
            else if ( type == typeof( object ) )
            {
                return source;
            }
            else if ( type.IsEnum )
            {
                return Enum.Parse( type, source.ToString() );
            }
            else if ( type.IsGenericType )
            {
                var baseArguments = type.GetGenericArguments();

                if ( baseArguments.Length == 2 && typeof( IDictionary ).IsAssignableFrom( type ) && source is ObjectInstance )
                {
                    var obj = ( ObjectInstance ) source;
                    var dictionary = ( IDictionary ) Activator.CreateInstance( type );

                    foreach ( var property in obj.Properties )
                    {
                        dictionary.Add( property.Key, CastInternal( property.Value, typeof( object ) ) );
                    }

                    return dictionary;
                }
                else if ( baseArguments.Length == 1 && type.IsAssignableFrom( typeof( List<> ).MakeGenericType( baseArguments[0] ) ) && source is ArrayInstance )
                {
                    var array = ( ArrayInstance ) source;
                    IList items;

                    items = ( IList ) Activator.CreateInstance( typeof( List<> ).MakeGenericType( baseArguments[0] ) );

                    foreach ( var v in array.ElementValues )
                    {
                        items.Add( CastInternal( v, baseArguments[0] ) );
                    }

                    return items;
                }
            }
            else if ( type.IsClass && source is ObjectInstance )
            {
                var obj = ( ObjectInstance ) source;
                var target = Activator.CreateInstance( type );

                var targetProperties = type.GetProperties( BindingFlags.Instance | BindingFlags.Public );

                foreach ( var property in obj.Properties )
                {
                    if ( !( property.Key is string ) )
                    {
                        continue;
                    }

                    var targetProperty = targetProperties.SingleOrDefault( p => p.Name.ToLowerInvariant() == ( ( string ) property.Key ).ToLowerInvariant() );

                    if ( targetProperty != null && targetProperty.CanWrite )
                    {
                        targetProperty.SetValue( target, CastInternal( property.Value, targetProperty.PropertyType ) );
                    }
                }

                return target;
            }

            throw new ArgumentException( $"Cannot convert to unsupported type {type.FullName}", nameof( type ) );
        }
    }
}
