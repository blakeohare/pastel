namespace PastelTest;

public class Program {

    public static void Main(string[] args) {
        try {
            PastelTest.GeneratedCode.FunctionWrapper.runner();
        } catch (System.Exception e) {
            System.Console.WriteLine("*FAIL!* -- c# hard crash");
            System.Console.WriteLine(e);
        }
    }
}
