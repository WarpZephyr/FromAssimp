using Assimp;
using SoulsFormats;
using System.IO;

namespace FromAssimp
{
    /// <summary>
    /// An <see cref="AssimpContext"/> wrapper for adding FromSoftware model support.
    /// </summary>
    public class FromAssimpContext
    {
        /// <summary>
        /// The underlying <see cref="AssimpContext"/>.
        /// </summary>
        public AssimpContext Context { get; set; }

        /// <summary>
        /// Create a new <see cref="FromAssimpContext"/>.
        /// </summary>
        public FromAssimpContext()
        {
            Context = new AssimpContext();
        }

        /// <summary>
        /// Import a model from the specified path.
        /// </summary>
        /// <param name="path">The path to import a model from.</param>
        /// <param name="postProcessFlags">The post processing steps to take while importing, only for non-FromSoftware models for now.</param>
        /// <returns>A <see cref="Scene"/>.</returns>
        public Scene ImportFile(string path, PostProcessSteps postProcessFlags = PostProcessSteps.None)
        {
            if (MDL4.IsRead(path, out MDL4 mdl4))
                return mdl4.ToAssimpScene();
            else if (SMD4.IsRead(path, out SMD4 smd4))
                return smd4.ToAssimpScene();
            else if (FLVER0.IsRead(path, out FLVER0 flver0))
                return flver0.ToAssimpScene();
            else if (FLVER2.IsRead(path, out FLVER2 flver2))
                return flver2.ToAssimpScene();
            else
                return Context.ImportFile(path, postProcessFlags);
        }

        /// <summary>
        /// Import a model from a <see cref="Stream"/>. Converts the <see cref="Stream"/> into a byte array for FromSoftware model reading.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to import a model from.</param>
        /// <param name="postProcessFlags">The post processing steps to take while importing, only for non-FromSoftware models for now.</param>
        /// <returns>A <see cref="Scene"/>.</returns>
        public Scene ImportFileFromStream(Stream stream, PostProcessSteps postProcessFlags = PostProcessSteps.None)
        {
            byte[] bytes;
            using (MemoryStream ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                ms.Seek(0, SeekOrigin.Begin);
                bytes = ms.ToArray();
            }

            if (MDL4.IsRead(bytes, out MDL4 mdl4))
                return mdl4.ToAssimpScene();
            else if (SMD4.IsRead(bytes, out SMD4 smd4))
                return smd4.ToAssimpScene();
            else if (FLVER0.IsRead(bytes, out FLVER0 flver0))
                return flver0.ToAssimpScene();
            else if (FLVER2.IsRead(bytes, out FLVER2 flver2))
                return flver2.ToAssimpScene();
            else
                return Context.ImportFileFromStream(stream, postProcessFlags);
        }

        /// <summary>
        /// Import a model from a byte array.
        /// </summary>
        /// <param name="bytes">The byte array to import a model from.</param>
        /// <param name="postProcessFlags">The post processing steps to take while importing, only for non-FromSoftware models for now.</param>
        /// <returns>A <see cref="Scene"/>.</returns>
        public Scene ImportFileFromBytes(byte[] bytes, PostProcessSteps postProcessFlags = PostProcessSteps.None)
        {
            if (MDL4.IsRead(bytes, out MDL4 mdl4))
                return mdl4.ToAssimpScene();
            else if (SMD4.IsRead(bytes, out SMD4 smd4))
                return smd4.ToAssimpScene();
            else if (FLVER0.IsRead(bytes, out FLVER0 flver0))
                return flver0.ToAssimpScene();
            else if (FLVER2.IsRead(bytes, out FLVER2 flver2))
                return flver2.ToAssimpScene();

            using (MemoryStream ms = new MemoryStream(bytes))
            {
                return Context.ImportFileFromStream(ms, postProcessFlags);
            }
        }

        public Scene ImportFileFromIFlver(IFlver model)
        {
            if (model is FLVER0 flver0)
                return flver0.ToAssimpScene();
            else if (model is FLVER2 flver2)
                return flver2.ToAssimpScene();
            else
                throw new NotSupportedException($"The type {model.GetType()} using the {nameof(IFlver)} interface is not supported for {nameof(ImportFileFromIFlver)}");
        }

        /// <summary>
        /// Import a file from a <see cref="FLVER0"/> model.
        /// </summary>
        /// <param name="model">The model to import from.</param>
        /// <returns>A <see cref="Scene"/>.</returns>
        public Scene ImportFileFromFlver0(FLVER0 model)
        {
            return model.ToAssimpScene();
        }

        /// <summary>
        /// Import a file from a <see cref="FLVER2"/> model.
        /// </summary>
        /// <param name="model">The model to import from.</param>
        /// <returns>A <see cref="Scene"/>.</returns>
        public Scene ImportFileFromFlver2(FLVER2 model)
        {
            return model.ToAssimpScene();
        }

        /// <summary>
        /// Import a file from a <see cref="MDL4"/> model.
        /// </summary>
        /// <param name="model">The model to import from.</param>
        /// <returns>A <see cref="Scene"/>.</returns>
        public Scene ImportFileFromMdl4(MDL4 model)
        {
            return model.ToAssimpScene();
        }

        /// <summary>
        /// Import a file from a <see cref="SMD4"/> model.
        /// </summary>
        /// <param name="model">The model to import from.</param>
        /// <returns>A <see cref="Scene"/>.</returns>
        public Scene ImportFileFromSmd4(SMD4 model)
        {
            return model.ToAssimpScene();
        }

        /// <summary>
        /// Import a file from a path, then export it to a path.
        /// </summary>
        /// <param name="inputPath">The path to import from.</param>
        /// <param name="outputPath">The path to export to.</param>
        /// <param name="exportFormatId">The format to write the exported file in.</param>
        /// <param name="postProcessFlags">The post processing steps to take while importing, only for non-FromSoftware models for now.</param>
        /// <returns>Whether or not exporting was successful.</returns>
        public bool ImportFileThenExport(string inputPath, string outputPath, string exportFormatId, PostProcessSteps postProcessFlags = PostProcessSteps.None)
        {
            return Context.ExportFile(ImportFile(inputPath, postProcessFlags), outputPath, exportFormatId);
        }

        /// <summary>
        /// Import a file from a <see cref="Stream"/>, then export it to a path.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to import from.</param>
        /// <param name="outputPath">The path to export to.</param>
        /// <param name="exportFormatId">The format to write the exported file in.</param>
        /// <param name="postProcessFlags">The post processing steps to take while importing, only for non-FromSoftware models for now.</param>
        /// <returns>Whether or not exporting was successful.</returns>
        public bool ImportFileFromStreamThenExport(Stream stream, string outputPath, string exportFormatId, PostProcessSteps postProcessFlags = PostProcessSteps.None)
        {
            return Context.ExportFile(ImportFileFromStream(stream, postProcessFlags), outputPath, exportFormatId);
        }

        /// <summary>
        /// Import a file from a byte array, then export it to a path.
        /// </summary>
        /// <param name="bytes">The byte array to import from.</param>
        /// <param name="outputPath">The path to export to.</param>
        /// <param name="exportFormatId">The format to write the exported file in.</param>
        /// <param name="postProcessFlags">The post processing steps to take while importing, only for non-FromSoftware models for now.</param>
        /// <returns>Whether or not exporting was successful.</returns>
        public bool ImportFileFromBytesThenExport(byte[] bytes, string outputPath, string exportFormatId, PostProcessSteps postProcessFlags = PostProcessSteps.None)
        {
            return Context.ExportFile(ImportFileFromBytes(bytes, postProcessFlags), outputPath, exportFormatId);
        }

        /// <summary>
        /// Import a file from a model type using the <see cref="IFlver"/> interface, then export it to a path. Only <see cref="FLVER0"/> and <see cref="FLVER2"/> are supported.
        /// </summary>
        /// <param name="model">The model to import from.</param>
        /// <param name="outputPath">The path to export to.</param>
        /// <param name="exportFormatId">The format to write the exported file in.</param>
        /// <returns>Whether or not exporting was successful.</returns>
        public bool ImportFileFromIFlverThenExport(IFlver model, string outputPath, string exportFormatId)
        {
            return Context.ExportFile(ImportFileFromIFlver(model), outputPath, exportFormatId);
        }

        /// <summary>
        /// Import a file from a <see cref="FLVER0"/> model, then export it to a path.
        /// </summary>
        /// <param name="model">The model to import from.</param>
        /// <param name="outputPath">The path to export to.</param>
        /// <param name="exportFormatId">The format to write the exported file in.</param>
        /// <returns>Whether or not exporting was successful.</returns>
        public bool ImportFileFromFlver0ThenExport(FLVER0 model, string outputPath, string exportFormatId)
        {
            return Context.ExportFile(ImportFileFromFlver0(model), outputPath, exportFormatId);
        }

        /// <summary>
        /// Import a file from a <see cref="FLVER2"/> model, then export it to a path.
        /// </summary>
        /// <param name="model">The model to import from.</param>
        /// <param name="outputPath">The path to export to.</param>
        /// <param name="exportFormatId">The format to write the exported file in.</param>
        /// <returns>Whether or not exporting was successful.</returns>
        public bool ImportFileFromFlver2ThenExport(FLVER2 model, string outputPath, string exportFormatId)
        {
            return Context.ExportFile(ImportFileFromFlver2(model), outputPath, exportFormatId);
        }

        /// <summary>
        /// Import a file from an <see cref="MDL4"/> model, then export it to a path.
        /// </summary>
        /// <param name="model">The model to import from.</param>
        /// <param name="outputPath">The path to export to.</param>
        /// <param name="exportFormatId">The format to write the exported file in.</param>
        /// <returns>Whether or not exporting was successful.</returns>
        public bool ImportFileFromMdl4ThenExport(MDL4 model, string outputPath, string exportFormatId)
        {
            return Context.ExportFile(ImportFileFromMdl4(model), outputPath, exportFormatId);
        }

        /// <summary>
        /// Import a file from an <see cref="SMD4"/> model, then export it to a path.
        /// </summary>
        /// <param name="model">The model to import from.</param>
        /// <param name="outputPath">The path to export to.</param>
        /// <param name="exportFormatId">The format to write the exported file in.</param>
        /// <returns>Whether or not exporting was successful.</returns>
        public bool ImportFileFromSmd4ThenExport(SMD4 model, string outputPath, string exportFormatId)
        {
            return Context.ExportFile(ImportFileFromSmd4(model), outputPath, exportFormatId);
        }

        /// <summary>
        /// Exports a scene to the specified format and writes it to a file. Currently does not support exporting From models.
        /// </summary>
        /// <param name="scene">The scene to export.</param>
        /// <param name="path">The path to write the exported file to.</param>
        /// <param name="exportFormatId">The format to write the exported file in.</param>
        /// <returns>Whether or not the export was successful.</returns>
        public bool ExportFile(Scene scene, string path, string exportFormatId)
        {
            return Context.ExportFile(scene, path, exportFormatId);
        }

        public List<string> GetSupportedExportOptions()
        {
            var strs = new List<string>();
            var formats = Context.GetSupportedExportFormats();
            foreach (var format in formats)
            {
                strs.Add(format.FormatId);
            }
            return strs;
        }

        public static string GetFormatExtension(string format)
        {
            return format switch
            {
                "fbx" => "fbx",
                "fbxa" => "fbx",
                "collada" => "dae",
                "obj" => "obj",
                _ => format
            };
        }
    }
}