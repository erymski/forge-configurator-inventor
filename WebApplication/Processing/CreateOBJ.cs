using WebApplication.Definitions;

namespace WebApplication.Processing
{
    /// <summary>
    /// OBJ Exporter from Inventor document.
    /// </summary>
    public class CreateOBJ : ForgeAppBase
    {
        public override string Id => nameof(CreateOBJ);
        public override string Description => "Generate OBJ from Inventor document";

        protected override string OutputUrl(ProcessingArgs projectData) => projectData.ObjUrl;
        protected override string OutputName => "OBJ";
        protected override bool IsOutputZip { get; } = true;

        /// <summary>
        /// Constructor.
        /// </summary>
        public CreateOBJ(Publisher publisher) : base(publisher) { }
    }
}
