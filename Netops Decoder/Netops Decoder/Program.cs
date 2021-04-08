using System;

namespace Netops_Decoder
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Input: ");
            string input = Console.ReadLine();
            var np = new NetopProtocolParser();
            Console.WriteLine(np.GetRawDataContent(input));
            
        }
    }
}
