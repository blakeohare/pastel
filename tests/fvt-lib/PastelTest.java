public class PastelTest {
  public static void main(String[] args) {
    FunctionWrapper.PST_RegisterExtensibleCallback("fail", new FunctionWrapper.PstExtWrapper() { 
      @Override 
      public Object run(Object[] args) {
        String msg = args[0] == null ? "null" : args[0].toString();
        throw new RuntimeException(msg);
      }
    });
    try {
      FunctionWrapper.runner();
    } catch (Exception ex) {
      System.out.println("FAIL!");
      System.out.println(ex);
    }
  }  
}
