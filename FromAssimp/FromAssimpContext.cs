using Assimp;
using SoulsFormats;
using FromAssimp.Import;
using FromAssimp.Helpers;
using FromAssimp.Extensions;

namespace FromAssimp
{
    /// <summary>
    /// An <see cref="AssimpContext"/> wrapper for adding FromSoftware model support.
    /// </summary>
    public class FromAssimpContext : IDisposable
    {
        /// <summary>
        /// The underlying <see cref="AssimpContext"/>.
        /// </summary>
        public AssimpContext Context { get; set; }

        /// <summary>
        /// Whether or not to do the check flip fix during FLVER0 face triangulation.
        /// </summary>
        public bool DoCheckFlip { get; set; }

        /// <summary>
        /// Whether or not to mirror across the X axis.
        /// </summary>
        public bool MirrorX { get; set; }

        /// <summary>
        /// Whether or not to mirror across the Y axis.
        /// </summary>
        public bool MirrorY { get; set; }

        /// <summary>
        /// Whether or not to mirror across the Z axis.
        /// </summary>
        public bool MirrorZ { get; set; }

        /// <summary>
        /// Whether or not to automatically convert units depending on the export format.
        /// </summary>
        public bool ConvertUnitSystem { get; set; }

        /// <summary>
        /// Whether or not to set unit system conversions into properties or metadata instead of manual conversions where possible.
        /// </summary>
        public bool PreferUnitSystemProperty { get; set; }

        /// <summary>
        /// The scale to import at.
        /// </summary>
        public float ImportScale
        {
            get => Context.Scale;
            set => Context.Scale = value;
        }

        /// <summary>
        /// The scale to export at.
        /// </summary>
        public float ExportScale { get; set; } = 1.0f;

        /// <summary>
        /// Whether or not the underlying <see cref="AssimpContext"/> has been disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Create a new <see cref="FromAssimpContext"/>.
        /// </summary>
        public FromAssimpContext()
        {
            Context = new AssimpContext();
        }

        /// <summary>
        /// Create a new <see cref="FromAssimpContext"/> from the given <see cref="AssimpContext"/>.
        /// </summary>
        public FromAssimpContext(AssimpContext context)
        {
            if (context.IsDisposed)
            {
                throw new InvalidOperationException($"{nameof(AssimpContext)} {nameof(context)} is already disposed.");
            }

            Context = context;
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
                return ImportFileFromMdl4(mdl4);
            else if (SMD4.IsRead(path, out SMD4 smd4))
                return ImportFileFromSmd4(smd4);
            else if (FLVER0.IsRead(path, out FLVER0 flver0))
                return ImportFileFromFlver0(flver0);
            else if (FLVER2.IsRead(path, out FLVER2 flver2))
                return ImportFileFromFlver2(flver2);

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
            if (MDL4.IsRead(stream, out MDL4 mdl4))
                return ImportFileFromMdl4(mdl4);
            else if (SMD4.IsRead(stream, out SMD4 smd4))
                return ImportFileFromSmd4(smd4);
            else if (FLVER0.IsRead(stream, out FLVER0 flver0))
                return ImportFileFromFlver0(flver0);
            else if (FLVER2.IsRead(stream, out FLVER2 flver2))
                return ImportFileFromFlver2(flver2);

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
                return ImportFileFromMdl4(mdl4);
            else if (SMD4.IsRead(bytes, out SMD4 smd4))
                return ImportFileFromSmd4(smd4);
            else if (FLVER0.IsRead(bytes, out FLVER0 flver0))
                return ImportFileFromFlver0(flver0);
            else if (FLVER2.IsRead(bytes, out FLVER2 flver2))
                return ImportFileFromFlver2(flver2);

            using var ms = new MemoryStream(bytes);
            return Context.ImportFileFromStream(ms, postProcessFlags);
        }

        /// <summary>
        /// Import a file from a <see cref="IFlver"/> model.
        /// </summary>
        /// <param name="model">The model to import from.</param>
        /// <returns>A <see cref="Scene"/>.</returns>
        /// <exception cref="NotSupportedException">The type using the <see cref="IFlver"/> interface was not supported.</exception>
        public Scene ImportFileFromIFlver(IFlver model)
        {
            if (model is FLVER0 flver0)
                return ImportFileFromFlver0(flver0);
            else if (model is FLVER2 flver2)
                return ImportFileFromFlver2(flver2);

            throw new NotSupportedException($"The type {model.GetType().Name} using the {nameof(IFlver)} interface is not supported for {nameof(ImportFileFromIFlver)}");
        }

        /// <summary>
        /// Import a file from a <see cref="FLVER0"/> model.
        /// </summary>
        /// <param name="model">The model to import from.</param>
        /// <returns>A <see cref="Scene"/>.</returns>
        public Scene ImportFileFromFlver0(FLVER0 model)
        {
            return FlverImporter.ImportFlver0(model, DoCheckFlip);
        }

        /// <summary>
        /// Import a file from a <see cref="FLVER2"/> model.
        /// </summary>
        /// <param name="model">The model to import from.</param>
        /// <returns>A <see cref="Scene"/>.</returns>
        public Scene ImportFileFromFlver2(FLVER2 model)
        {
            return FlverImporter.ImportFlver2(model);
        }

        /// <summary>
        /// Import a file from a <see cref="MDL4"/> model.
        /// </summary>
        /// <param name="model">The model to import from.</param>
        /// <returns>A <see cref="Scene"/>.</returns>
        public Scene ImportFileFromMdl4(MDL4 model)
        {
            return Mdl4Importer.ImportMdl4(model);
        }

        /// <summary>
        /// Import a file from a <see cref="SMD4"/> model.
        /// </summary>
        /// <param name="model">The model to import from.</param>
        /// <returns>A <see cref="Scene"/>.</returns>
        public Scene ImportFileFromSmd4(SMD4 model)
        {
            return Smd4Importer.ImportSmd4(model);
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
            return ExportFile(scene, path, exportFormatId, PostProcessSteps.None);
        }

        /// <summary>
        /// Exports a scene to the specified format and writes it to a file. Currently does not support exporting From models.
        /// </summary>
        /// <param name="scene">The scene to export.</param>
        /// <param name="path">The path to write the exported file to.</param>
        /// <param name="exportFormatId">The format to write the exported file in.</param>
        /// <param name="preProcessing">Preprocessing flags to apply to the model before it is exported.</param>
        /// <returns>Whether or not the export was successful.</returns>
        public bool ExportFile(Scene scene, string path, string exportFormatId, PostProcessSteps preProcessing)
        {
            float scale = ExportScale;
            if (ConvertUnitSystem && IsFbxFormat(exportFormatId))
            {
                if (PreferUnitSystemProperty)
                {
                    scene.Metadata.TryAdd("UnitScaleFactor", new Metadata.Entry(MetaDataType.Double, TransformHelper.FbxUnitMetersDouble));
                }
                else
                {
                    scale *= TransformHelper.FbxUnitMeters;
                }
            }

            if (scale != 1.0f)
                ScaleHelper.ScaleSceneUniform(scene, scale);

            if (MirrorX || MirrorY || MirrorZ)
            {
                var mirror = TransformHelper.GetMirrorMatrix(MirrorX, MirrorY, MirrorZ).ToAssimpMatrix4x4();
                MirrorHelper.MirrorScene(scene, mirror);
            }

            return Context.ExportFile(scene, path, exportFormatId);
        }

        /// <summary>
        /// Get the appropiate extension for the chosen format.
        /// </summary>
        /// <param name="format">The format to get the extension of.</param>
        /// <returns>The extension of the given format.</returns>
        public static string GetFormatExtension(string format)
        {
            return format.ToLowerInvariant() switch
            {
                "fbx" => "fbx",
                "fbxa" => "fbx",
                "collada" => "dae",
                "obj" => "obj",
                _ => format
            };
        }

        /// <summary>
        /// Whether or not the specified format is an FBX format.
        /// </summary>
        /// <param name="format">The format to check.</param>
        /// <returns>Whether or not the specified format is an FBX format.</returns>
        public static bool IsFbxFormat(string format)
        {
            var value = format.ToLowerInvariant();
            return value switch
            {
                "fbx" => true,
                "fbxa" => true,
                _ => value.Contains("fbx")
            };
        }

        #region IDisposable Implementation

        /// <summary>
        /// Disposes the underlying <see cref="AssimpContext"/>.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    Context.Dispose();
                }

                IsDisposed = true;
            }
        }

        /// <summary>
        /// Disposes the underlying <see cref="AssimpContext"/>.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}