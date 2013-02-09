namespace SlowCheetah.Xdt {
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using Microsoft.Web.XmlTransform;
    using System;

    public class TransformXml : Task {
        private string _transformRootPath = string.Empty;
        private bool stackTrace = false;

        [Required]
        public ITaskItem Source {
            get;
            set;
        }

        public string SourceRootPath {
            get;
            set;
        }


        [Required]
        public ITaskItem Transform {
            get;
            set;
        }

        public string TransformRootPath {
            get {
                if (string.IsNullOrEmpty(_transformRootPath)) {
                    return this.SourceRootPath;
                }
                else {
                    return _transformRootPath;
                }
            }
            set { _transformRootPath = value; }
        }


        [Required]
        public ITaskItem Destination {
            get;
            set;
        }

        public bool StackTrace {
            get {
                return stackTrace;
            }
            set {
                stackTrace = value;
            }
        }

        public override bool Execute() {
            bool succeeded = true;
            XmlTransformation transformation = null;
            XmlTransformableDocument document = null;

            try {
                Log.LogMessage(MessageImportance.Low, "Transfroming source file: {0}", Source);
                document = OpenSourceFile(Source.GetMetadata("FullPath"));

                Log.LogMessage(MessageImportance.Low, "Applying Transform File: {0}", Transform);
                transformation = OpenTransformFile(Transform.GetMetadata("FullPath"), null);

                succeeded = transformation.Apply(document);

                if (succeeded) {
                    Log.LogMessage(MessageImportance.Low, "Output File: {0}", Destination);
                    SaveTransformedFile(document, Destination.GetMetadata("FullPath"));
                }
            }
            catch (System.Xml.XmlException ex) {
                string localPath = Source.GetMetadata("FullPath");
                if (!string.IsNullOrEmpty(ex.SourceUri)) {
                    Uri sourceUri = new Uri(ex.SourceUri);
                    localPath = sourceUri.LocalPath;
                }

                Log.LogErrorFromException(ex);
                succeeded = false;
            }
            catch (Exception ex) {
                Log.LogErrorFromException(ex);
                succeeded = false;
            }
            finally {
                if (transformation != null) {
                    transformation.Dispose();
                }
                if (document != null) {
                    document.Dispose();
                }
            }

            return succeeded;
        }

        private void SaveTransformedFile(XmlTransformableDocument document, string destinationFile) {
            try {
                document.Save(destinationFile);
            }
            catch (System.Xml.XmlException) {
                throw;
            }
            catch (Exception ex) {
                throw new Exception(string.Format("Could not write Destination file: {0}", ex.Message), ex);
            }
        }

        private XmlTransformableDocument OpenSourceFile(string sourceFile) {
            try {
                XmlTransformableDocument document = new XmlTransformableDocument();

                document.PreserveWhitespace = true;
                document.Load(sourceFile);

                return document;
            }
            catch (System.Xml.XmlException) {
                throw;
            }
            catch (Exception ex) {
                throw new Exception(
                    string.Format(System.Globalization.CultureInfo.CurrentCulture,
                    "Could not open Source file: {0}", ex.Message),
                    ex);
            }
        }

        private XmlTransformation OpenTransformFile(string transformFile, IXmlTransformationLogger logger) {
            try {
                return new XmlTransformation(transformFile, logger);
            }
            catch (System.Xml.XmlException) {
                throw;
            }
            catch (Exception ex) {
                throw new Exception(
                    string.Format(System.Globalization.CultureInfo.CurrentCulture,
                    "Could not open Transform file: {0}", ex.Message),
                    ex);
            }
        }
    }
}
