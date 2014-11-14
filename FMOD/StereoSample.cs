namespace FMOD
{
    public struct StereoSample
    {
        public StereoSample(float mono)
        {
            Left = mono;
            Right = mono;
        }

        public StereoSample(float left, float right)
        {
            Left = left;
            Right = right;
        }

        public float Left;
        public float Right;
    }
}
