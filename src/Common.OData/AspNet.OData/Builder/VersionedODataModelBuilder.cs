﻿namespace Microsoft.AspNet.OData.Builder
{
#if !WEBAPI
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Versioning;
#endif
    using Microsoft.OData.Edm;
#if WEBAPI
    using Microsoft.Web.Http;
    using Microsoft.Web.Http.Versioning;
#endif
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;

    /// <summary>
    /// Represents a versioned variant of the <see cref="ODataModelBuilder"/>.
    /// </summary>
    public partial class VersionedODataModelBuilder
    {
        Func<ODataModelBuilder> modelBuilderFactory = () => new ODataConventionModelBuilder();

        /// <summary>
        /// Gets or sets the factory method used to create model builders.
        /// </summary>
        /// <value>The factory <see cref="Func{TResult}">method</see> used to create <see cref="ODataModelBuilder">model builders</see>.</value>
        /// <remarks>The default implementation creates default instances of the <see cref="ODataConventionModelBuilder"/> class.</remarks>
        public Func<ODataModelBuilder> ModelBuilderFactory
        {
            get
            {
                Contract.Ensures( modelBuilderFactory != null );
                return modelBuilderFactory;
            }
            set
            {
                Arg.NotNull( value, nameof( value ) );
                modelBuilderFactory = value;
            }
        }

        /// <summary>
        /// Gets or sets the default model configuration.
        /// </summary>
        /// <value>The <see cref="Action{T1, T2}">method</see> for the default model configuration.
        /// The default value is <c>null</c>.</value>
        public Action<ODataModelBuilder, ApiVersion> DefaultModelConfiguration { get; set; }

        /// <summary>
        /// Gets the list of model configurations associated with the builder.
        /// </summary>
        /// <value>A <see cref="IList{T}">list</see> of model configurations associated with the builder.</value>
        public IList<IModelConfiguration> ModelConfigurations { get; } = new List<IModelConfiguration>();

        /// <summary>
        /// Gets or sets the action that is invoked after the <see cref="IEdmModel">EDM model</see> has been created.
        /// </summary>
        /// <value>The <see cref="Action{T1,T2}">action</see> to run after the model has been created. The default
        /// value is <c>null</c>.</value>
        public Action<ODataModelBuilder, IEdmModel> OnModelCreated { get; set; }

        IEnumerable<IModelConfiguration> GetMergedConfigurations()
        {
            Contract.Ensures( Contract.Result<IEnumerable<IModelConfiguration>>() != null );

            var defaultConfiguration = DefaultModelConfiguration;

            if ( defaultConfiguration == null )
            {
                return ModelConfigurations;
            }

            var configurations = new IModelConfiguration[ModelConfigurations.Count + 1];

            configurations[0] = new DelegatingModelConfiguration( defaultConfiguration );
            ModelConfigurations.CopyTo( configurations, 1 );

            return configurations;
        }

        void BuildModelPerApiVersion( IEnumerable<ApiVersion> apiVersions, IEnumerable<IModelConfiguration> configurations, ICollection<IEdmModel> models )
        {
            Contract.Requires( apiVersions != null );
            Contract.Requires( configurations != null );
            Contract.Requires( models != null );

            foreach ( var apiVersion in apiVersions )
            {
                var builder = ModelBuilderFactory();

                foreach ( var configuration in configurations )
                {
                    configuration.Apply( builder, apiVersion );
                }

                var model = builder.GetEdmModel();

                model.SetAnnotationValue( model, new ApiVersionAnnotation( apiVersion ) );
                OnModelCreated?.Invoke( builder, model );
                models.Add( model );
            }
        }
    }
}