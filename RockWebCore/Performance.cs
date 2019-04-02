using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

namespace RockWebCore
{
    public static class Performance
    {
        #region DI Performance Testing

        public static void DITest()
        {
            var services = new ServiceCollection();

            services.AddTransient<TestClass>();

            var provider = services.BuildServiceProvider( true );

            var cc = new CommonClass();

            var sw = System.Diagnostics.Stopwatch.StartNew();
            for ( int i = 0; i < 1000000; i++ )
            {
                var di = ActivatorUtilities.CreateInstance<DIClass>( provider, cc );
            }
            sw.Stop();
            var diTime = sw.ElapsedMilliseconds;

            sw = System.Diagnostics.Stopwatch.StartNew();
            for ( int i = 0; i < 1000000; i++ )
            {
                var di = Activator.CreateInstance( typeof( DIClass ), cc, new TestClass() );
            }
            sw.Stop();
            var actTime = sw.ElapsedMilliseconds;

            sw = System.Diagnostics.Stopwatch.StartNew();
            for ( int i = 0; i < 1000000; i++ )
            {
                var di = new DIClass( cc, new TestClass() );
            }
            sw.Stop();
            var stdTime = sw.ElapsedMilliseconds;
        }

        protected class CommonClass
        {
        }

        protected class DIClass
        {
            public TestClass TC { get; set; }

            public DIClass( CommonClass cc, TestClass tc )
            {
                tc.Value = 42;
                TC = tc;
            }
        }

        protected class TestClass
        {
            public int Value { get; set; }
        }

        #endregion

        #region Testing

        //
        // These methods are used to verify that AsyncLocal<> works both on
        // threads as well as async contexts. The answer is YES it does.
        // By calling GC.Collect() after calling TestAsyncLocal() it will
        // also prove that AsyncLocal<> will automatically drop references
        // to the objects once they go out of context without us having to
        // set the AsyncLocal<> value to null.
        //

        private static void TestAsyncLocal()
        {
            void threadAction()
            {
                Counter.Current = new Counter();
                ProcessCounter();
                Console.WriteLine( $"Counter {Counter.Current.Id} = {Counter.Current.Value}" );
            };

            var threads = new List<Thread>();
            for ( int i = 0; i < 10; i++ )
            {
                threads.Add( new Thread( threadAction ) );
            }

            Console.WriteLine( "Testing with threads..." );
            threads.ForEach( t => t.Start() );
            while ( threads.Any( t => t.IsAlive ) )
            {
                Thread.Sleep( 100 );
            }
            Console.WriteLine();

            async Task asyncAction()
            {
                Counter.Current = new Counter();
                await ProcessCounterAsync();
                Console.WriteLine( $"Counter {Counter.Current.Id} = {Counter.Current.Value}" );
            }

            Console.WriteLine( "Testing with tasks..." );
            var tasks = new List<Task>();
            for ( int i = 0; i < 10; i++ )
            {
                tasks.Add( Task.Run( async () =>
                {
                    await asyncAction();
                } ) );
            }
            Task.WhenAll( tasks ).Wait();
            Console.WriteLine();
        }

        private static void ProcessCounter()
        {
            for ( int i = 0; i < 1000; i++ )
            {
                Counter.Current.Value += 1;
                Thread.Sleep( 10 );
            }
        }

        private static async Task ProcessCounterAsync()
        {
            for ( int i = 0; i < 1000; i++ )
            {
                Counter.Current.Value += 1;
                await Task.Delay( 10 );
            }
        }

        public class Counter : IDisposable
        {
            private static int _count = 0;
            private static object _lock = new object();

            public Guid Id { get; } = Guid.NewGuid();

            public int Value { get; set; }

            public static Counter Current
            {
                get
                {
                    return _current.Value;
                }
                set
                {
                    _current.Value = value;
                }
            }
            private static readonly AsyncLocal<Counter> _current = new AsyncLocal<Counter>();

            public Counter()
            {
                lock ( _lock )
                {
                    _count += 1;
                    Console.WriteLine( $"Initialized Counter {Id} ({_count})" );
                }
            }

            ~Counter()
            {
                lock ( _lock )
                {
                    Console.WriteLine( $"Destroyed Counter {Id} ({_count})" );
                    _count -= 1;
                }
            }

            public void Dispose()
            {
                throw new NotImplementedException();
            }
        }

        #endregion
    }
}
