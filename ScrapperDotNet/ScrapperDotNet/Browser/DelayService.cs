using System.Security.Cryptography;

namespace ScrapperDotNet.Browser
{
    internal class DelayService
    {
        // consider using a selenium awaiter instead:
        // https://www.browserstack.com/guide/selenium-wait-for-page-to-load
        public static void WaitSplitASec()
        {
            int delay = RandomNumberGenerator.GetInt32(50, 500);
            Thread.Sleep(delay);
        }
        public static Task WaitSplitASecAsync()
        {
            int delay = RandomNumberGenerator.GetInt32(50, 500);
            return Task.Delay(delay);
        }

        public static void WaitHalfASec()
        {
            int delay = RandomNumberGenerator.GetInt32(400, 600);
            Thread.Sleep(delay);
        }

        public static Task WaitHalfASecAsync()
        {
            int delay = RandomNumberGenerator.GetInt32(400, 600);
            return Task.Delay(delay);
        }

        public static void WaitASec() => SleepRange(1, 2);
        public static Task WaitASecAsync() => SleepRangeAsync(1, 2);

        public static void WaitAFewSec() => SleepRange(4, 12);
        public static Task WaitAFewSecAsync() => SleepRangeAsync(4, 12);

        public static void WaitHalfAMin() => SleepRange(20, 40);
        public static Task WaitHalfAMinAsync() => SleepRangeAsync(20, 40);

        public static void SleepRange(int minSeconds, int maxSeconds)
        {
            int delay = GetDelay(minSeconds, maxSeconds);
            Thread.Sleep(delay);
        }

        public static Task SleepRangeAsync(int minSeconds, int maxSeconds)
        {
            int delay = GetDelay(minSeconds, maxSeconds);
            return Task.Delay(delay);
        }

        private static int GetDelay(int minSeconds, int maxSeconds)
        {
            return RandomNumberGenerator.GetInt32(minSeconds * 1000, maxSeconds * 1000);
        }

        public static bool WaitForUserAction()
        {
            Console.Beep(4000, 100);
            Console.Beep(3700, 100);
            Console.Beep(3400, 100);
            Console.Beep(3000, 500);
            Console.Beep(2800, 1000);

            Console.WriteLine("Hit <Enter> to continue or <Esc> to skip.");

            var keyPressed = Console.ReadKey().Key;
            while (true)
            {
                switch (keyPressed)
                {
                    case ConsoleKey.Enter: Console.WriteLine("Carrying on..."); return true;
                    case ConsoleKey.Escape: Console.WriteLine("Skipping..."); return false;
                    default:
                        {
                            Console.WriteLine("Wrong key. Hit Enter or Esc.");
                            keyPressed = Console.ReadKey().Key;
                        }
                        break;
                }
            }
        }
    }
}
