namespace FromAssimp.Extensions.Common
{
    public static class ArrayExtensions
    {
        public static int[] ToIntArray(this short[] array)
        {
            int[] result = new int[array.Length];
            for (int i = 0; i < array.Length; i++)
            {
                result[i] = array[i];
            }
            return result;
        }
    }
}
