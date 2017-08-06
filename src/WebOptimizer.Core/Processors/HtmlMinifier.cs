﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using NUglify;
using NUglify.Css;
using NUglify.Html;

namespace WebOptimizer
{
    /// <summary>
    /// A processor that minifies JavaScript
    /// </summary>
    internal class HtmlMinifier : IProcessor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CssMinifier"/> class.
        /// </summary>
        public HtmlMinifier(HtmlSettings settings)
        {
            Settings = settings;
        }

        /// <summary>
        /// Gets the custom key that should be used when calculating the memory cache key.
        /// </summary>
        public string CacheKey(HttpContext context) => string.Empty;

        /// <summary>
        /// Gets or sets the settings.
        /// </summary>
        public HtmlSettings Settings { get; set; }

        /// <summary>
        /// Executes the processor on the specified configuration.
        /// </summary>
        public Task ExecuteAsync(IAssetContext config)
        {
            var content = new Dictionary<string, byte[]>();

            foreach (string key in config.Content.Keys)
            {
                if (key.EndsWith(".min.html"))
                    continue;

                UglifyResult result = Uglify.Html(config.Content[key].AsString(), Settings);
                string minified = result.Code;

                if (result.HasErrors)
                {
                    minified = $"/* {string.Join("\r\n", result.Errors)} */";
                }

                content[key] = minified.AsByteArray();
            }

            config.Content = content;

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Extension methods for <see cref="IAssetPipeline"/>.
    /// </summary>
    public static partial class PipelineExtensions
    {
        /// <summary>
        /// Minifies and fingerprints any .html file requested.
        /// </summary>
        public static IAsset MinifyHtmlFiles(this IAssetPipeline pipeline) =>
            pipeline.MinifyHtmlFiles(new HtmlSettings());

        /// <summary>
        /// Minifies and fingerprints any .html file requested.
        /// </summary>
        public static IAsset MinifyHtmlFiles(this IAssetPipeline pipeline, HtmlSettings settings)
        {
            return pipeline.AddFileExtension(".html", "text/html; charset=UTF-8")
                           .MinifyHtml(settings);
        }


        /// <summary>
        /// Minifies the specified .html files
        /// </summary>
        public static IEnumerable<IAsset> MinifyHtmlFiles(this IAssetPipeline pipeline, params string[] sourceFiles)
        {
            return pipeline.MinifyHtmlFiles(new HtmlSettings(), sourceFiles);
        }

        /// <summary>
        /// Minifies the specified .html files
        /// </summary>
        public static IEnumerable<IAsset> MinifyHtmlFiles(this IAssetPipeline pipeline, HtmlSettings settings, params string[] sourceFiles)
        {
            return pipeline.AddFiles("text/html; charset=UTF-8", sourceFiles)
                           .MinifyHtml(settings);
        }

        /// <summary>
        /// Creates a HTML bundle on the specified route and minifies the output.
        /// </summary>
        public static IAsset AddHtmlBundle(this IAssetPipeline pipeline, string route, params string[] sourceFiles)
        {
            return pipeline.AddHtmlBundle(route, new HtmlSettings(), sourceFiles);
        }

        /// <summary>
        /// Creates a HTML bundle on the specified route and minifies the output.
        /// </summary>
        public static IAsset AddHtmlBundle(this IAssetPipeline pipeline, string route, HtmlSettings settings, params string[] sourceFiles)
        {
            return pipeline.AddBundle(route, "text/html; charset=UTF-8", sourceFiles)
                           .Concatinate()
                           .MinifyHtml(settings);
        }

        /// <summary>
        /// Runs the HTML minifier on the content.
        /// </summary>
        public static IAsset MinifyHtml(this IAsset bundle)
        {
            return bundle.MinifyHtml(new HtmlSettings());
        }

        /// <summary>
        /// Runs the HTML minifier on the content.
        /// </summary>
        public static IAsset MinifyHtml(this IAsset bundle, HtmlSettings settings)
        {
            var minifier = new HtmlMinifier(settings);
            bundle.Processors.Add(minifier);

            return bundle;
        }

        /// <summary>
        /// Runs the HTML minifier on the content.
        /// </summary>
        public static IEnumerable<IAsset> MinifyHtml(this IEnumerable<IAsset> assets)
        {
            return assets.MinifyHtml(new HtmlSettings());
        }

        /// <summary>
        /// Runs the HTML minifier on the content.
        /// </summary>
        public static IEnumerable<IAsset> MinifyHtml(this IEnumerable<IAsset> assets, HtmlSettings settings)
        {
            var list = new List<IAsset>();

            foreach (IAsset asset in assets)
            {
                list.Add(asset.MinifyHtml(settings));
            }

            return list;
        }
    }
}
