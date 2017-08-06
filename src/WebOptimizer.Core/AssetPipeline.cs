﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace WebOptimizer
{
    internal class AssetPipeline : IAssetPipeline
    {
        private List<IAsset> _assets = new List<IAsset>();

        public bool? EnableTagHelperBundling { get; set; }

        public bool? UseContentRoot { get; set; }

        public IReadOnlyList<IAsset> Assets => _assets;

        public IFileProvider FileProvider { get; set; }

        public bool TryFromRoute(string route, out IAsset asset)
        {
            asset = null;
            string cleanRoute = route.TrimStart('~');

            foreach (IAsset existing in Assets)
            {
                if (existing.Route.Equals(cleanRoute, StringComparison.OrdinalIgnoreCase))
                {
                    asset = existing;
                    return true;
                }
            }

            foreach (IAsset existing in Assets.Where(a => a.Route[0] == '.'))
            {
                if (route.EndsWith(existing.Route, StringComparison.OrdinalIgnoreCase))
                {
                    asset = Asset.Create(cleanRoute, existing.ContentType, new[] { cleanRoute });

                    foreach (IProcessor processor in existing.Processors)
                    {
                        asset.Processors.Add(processor);
                    }

                    _assets.Add(asset);
                    return true;
                }
            }

            return false;
        }

        public IAsset AddBundle(IAsset asset)
        {
            return AddBundle(asset.Route, asset.ContentType, asset.SourceFiles.ToArray());
        }

        public IEnumerable<IAsset> AddBundle(IEnumerable<IAsset> assets)
        {
            var list = new List<IAsset>();

            foreach (IAsset asset in assets)
            {
                IAsset ass = AddBundle(asset.Route, asset.ContentType, asset.SourceFiles.ToArray());
                list.Add(ass);
            }

            return list;
        }

        public IAsset AddBundle(string route, string contentType, params string[] sourceFiles)
        {
            if (!route.StartsWith("/") && !route.StartsWith("."))
            {
                throw new ArgumentException($"The route \"{route}\" must start with a / or a .", nameof(route));
            }

            if (Assets.Any(a => a.Route.Equals(route, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException($"The route \"{route}\" was already specified", nameof(route));
            }

            string[] sources = sourceFiles;

            if (!route.StartsWith(".") && sourceFiles.Length == 0)
            {
                sources = new[] { route };
            }

            IAsset asset = Asset.Create(route, contentType, sources);
            _assets.Add(asset);

            return asset;
        }

        /// <summary>
        /// Compiles the specified .scss files into CSS and makes them servable in the browser.
        /// </summary>
        /// <param name="contentType">The content type of the response. Example: text/css or application/javascript.</param>
        /// <param name="sourceFiles">A list of relative file names of the sources to compile.</param>
        public IEnumerable<IAsset> AddFiles(string contentType, params string[] sourceFiles)
        {
            var list = new List<IAsset>();

            foreach (string file in sourceFiles)
            {
                IAsset asset = AddBundle(file, contentType, new[] { file });

                list.Add(asset);
            }

            return list;
        }

        /// <summary>
        /// Adds the file extension.
        /// </summary>
        /// <param name="extension">The extension to use. Example: .css or .js</param>
        /// <param name="contentType">The content type of the response. Example: text/css or application/javascript.</param>
        public IAsset AddFileExtension(string extension, string contentType)
        {
            IAsset asset = Asset.Create(extension, contentType, Enumerable.Empty<string>());
            return AddBundle(asset);
        }
    }

    /// <summary>
    /// Extension methods for <see cref="IAssetPipeline"/>.
    /// </summary>
    public static partial class PipelineExtensions
    {
        /// <summary>
        /// Adds WebOptimizer to the specified <see cref="IServiceCollection"/> and enables CSS and JavaScript minification.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="minifyJavaScript">If <code>true</code>; calls <code>AddJs()</code> on the pipeline.</param>
        /// <param name="minifyCss">If <code>true</code>; calls <code>AddCss()</code> on the pipeline.</param>
        public static IAssetPipeline AddWebOptimizer(this IServiceCollection services, bool minifyJavaScript = true, bool minifyCss = true)
        {
            var pipeline = new AssetPipeline();

            if (minifyCss)
            {
                pipeline.MinifyCssFiles();
            }

            if (minifyJavaScript)
            {
                pipeline.MinifyJsFiles();
            }

            services.AddSingleton<IAssetPipeline, AssetPipeline>(factory => pipeline);

            return pipeline;
        }

        /// <summary>
        /// Adds WebOptimizer to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        public static IAssetPipeline AddWebOptimizer(this IServiceCollection services, Action<IAssetPipeline> assetPipeline)
        {
            var pipeline = new AssetPipeline();
            assetPipeline(pipeline);

            services.AddSingleton<IAssetPipeline, AssetPipeline>(factory => pipeline);

            return pipeline;
        }

        /// <summary>
        /// Ensures that defaults are set
        /// </summary>
        public static void EnsureDefaults(this IAssetPipeline pipeline, IHostingEnvironment env)
        {
            pipeline.FileProvider = pipeline.FileProvider ?? (pipeline.UseContentRoot == true ? env.ContentRootFileProvider : env.WebRootFileProvider);
            pipeline.EnableTagHelperBundling = pipeline.EnableTagHelperBundling ?? !env.IsDevelopment();
        }
    }
}
