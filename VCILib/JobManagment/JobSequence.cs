namespace VCILib.JobManagment
{
    public class JobSequence
    {
        public UDSTimingParams TimingParameters { get; set; }
        public List<JobSequenceItem> Items { get; set; }=new List<JobSequenceItem>();

        private byte[][] seed;
        internal byte[][] Seed
        {
            get
            {
                if (seed == null) seed = new byte[3][];
                return seed;
            }
        }
    }
}
