﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Drawing.Design;

namespace TvEngine.MediaPortalIptvFilterAndUrlSourceSplitter.Url
{
    /// <summary>
    /// Represent base class for all urls.
    /// </summary>
    internal abstract class SimpleUrl
    {
        #region Private fields

        private Uri uri;
        private LogVerbosity verbosity = SimpleUrl.DefaultVerbosity;
        private String networkInterface = NetworkInterfaceConverter.NetworkInterfaceSystemDefault;
        private String cacheFolder = String.Empty;
        private int maximumLogSize = SimpleUrl.DefaultLogMaximumSize;

        private Mpeg2TsParser mpeg2TsParser = new Mpeg2TsParser();

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="SimpleUrl"/> class with specified URL.
        /// </summary>
        /// <param name="url">The URL to initialize.</param>
        /// <overloads>
        /// Initializes a new instance of <see cref="SimpleUrl"/> class.
        /// </overloads>
        public SimpleUrl(String url)
            : this(new Uri(url))
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="SimpleUrl"/> class with specified uniform resource identifier.
        /// </summary>
        /// <param name="uri">The uniform resource identifier.</param>
        /// <exception cref="ArgumentNullException">
        /// <para>The <paramref name="uri"/> is <see langword="null"/>.</para>
        /// </exception>
        public SimpleUrl(Uri uri)
        {
            this.Uri = uri;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the uniform resource identifier.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// <para>The <see cref="Uri"/> is <see langword="null"/>.</para>
        /// </exception>
        [Category("Common"), Description("The URL to open in IPTV filter.")]
        [Browsable(false)]
        public Uri Uri
        {
            get { return this.uri; }
            protected set
            {
                // check if value is not null
                if (value == null)
                {
                    throw new ArgumentNullException("Uri");
                }

                this.uri = value;
            }
        }

        /// <summary>
        /// Gets or sets the uniform resource identifier.
        /// </summary>
        [Category("Common"), Description("The URL to open in IPTV filter.")]
        public String Url
        {
            get { return this.Uri.ToString();}
            set { this.Uri = new System.Uri(value); }
        }

        /// <summary>
        /// Gets or sets the verbosity level.
        /// </summary>
        [Category("Common"), Description("The log verbosity."), DefaultValue(SimpleUrl.DefaultVerbosity)]
        public LogVerbosity Verbosity
        {
            get { return this.verbosity; }
            set
            {
                this.verbosity = value;
            }
        }

        /// <summary>
        /// Gets or sets the network interface.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// <para>The <see cref="NetworkInterface"/> is <see langword="null"/>.</para>
        /// </exception>
        /// <remarks>
        /// The network interface have to be name of network interface card. The empty string ("") means the default system network interface.
        /// </remarks>
        [Category("Common"), Description("Preferred network interface."), DefaultValue(NetworkInterfaceConverter.NetworkInterfaceSystemDefault)]
        [TypeConverter(typeof(NetworkInterfaceConverter))]
        public String NetworkInterface
        {
            get { return this.networkInterface; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("NetworkInterface");
                }

                this.networkInterface = value;
            }
        }

        /// <summary>
        /// Gets or sets maximum log size.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <para>The <see cref="MaximumLogSize"/> is less than zero.</para>
        /// </exception>
        [Category("Common"), Description("Maximum log size."), DefaultValue(SimpleUrl.DefaultLogMaximumSize)]
        public int MaximumLogSize
        {
            get { return this.maximumLogSize; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("MaximumLogSize", value, "Value cannot be less than zero.");
                }

                this.maximumLogSize = value;
            }
        }

        /// <summary>
        /// Gets or sets the cache folder.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// <para>The <see cref="CacheFolder"/> is <see langword="null"/>.</para>
        /// </exception>
        /// <remarks>
        /// The empty string ("") means the default cache folder.
        /// </remarks>
        [Category("Common"), Description("The cache folder to store IPTV filter temporary data. Do not use network paths, prefer paths on faster drives.")]
        [Editor(typeof(CacheFolderEditor), typeof(UITypeEditor))]
        public String CacheFolder
        {
            get { return this.cacheFolder; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("CacheFolder");
                }

                this.cacheFolder = value;
            }
        }

        /// <summary>
        /// Gets or sets the MPEG2 TS parser settings.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// <para>The <see cref="Mpeg2TsParser"/> is <see langword="null"/>.</para>
        /// </exception>
        [TypeConverter(typeof(Mpeg2TsParserConverter))]
        [Category("MPEG2 Transport Stream parser"), Description("The settings for MPEG2 Transport Stream parser.")]
        public Mpeg2TsParser Mpeg2TsParser
        {
            get { return this.mpeg2TsParser; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("Mpeg2TsParser");
                }

                this.mpeg2TsParser = value;
            }
        }

        /// <summary>
        /// Specifies if protocol have to dump input data.
        /// </summary>
        [Category("Debug options"), Description("Specifies if protocol have to dump input data."), DefaultValue(SimpleUrl.DefaultDumpProtocolInputData)]
        public Boolean DumpProtocolInputData { get; set; }

        /// <summary>
        /// Specifies if protocol have to dump output data.
        /// </summary>
        [Category("Debug options"), Description("Specifies if protocol have to dump output data."), DefaultValue(SimpleUrl.DefaultDumpProtocolOutputData)]
        public Boolean DumpProtocolOutputData { get; set; }

        /// <summary>
        /// Specifies if parser have to dump input data.
        /// </summary>
        [Category("Debug options"), Description("Specifies if parser have to dump input data."), DefaultValue(SimpleUrl.DefaultDumpParserInputData)]
        public Boolean DumpParserInputData { get; set; }

        /// <summary>
        /// Specifies if parser have to dump output data.
        /// </summary>
        [Category("Debug options"), Description("Specifies if parser have to dump output data."), DefaultValue(SimpleUrl.DefaultDumpParserOutputData)]
        public Boolean DumpParserOutputData { get; set; }

        /// <summary>
        /// Specifies if output pin(s) have to dump data.
        /// </summary>
        [Category("Debug options"), Description("Specifies if output pin(s) have to dump data."), DefaultValue(SimpleUrl.DefaultDumpOutputPinData)]
        public Boolean DumpOutputPinData { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Gets canonical string representation for the specified instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> instance that contains the unescaped canonical representation of the this instance.
        /// </returns>
        public override string ToString()
        {
            ParameterCollection parameters = new ParameterCollection();

            parameters.Add(new Parameter(SimpleUrl.ParameterUrl, this.Uri.ToString()));
            if (this.Verbosity != DefaultVerbosity)
            {
                parameters.Add(new Parameter(SimpleUrl.ParameterLogVerbosity, ((int)this.Verbosity).ToString()));
            }
            if (this.MaximumLogSize != SimpleUrl.DefaultLogMaximumSize)
            {
                parameters.Add(new Parameter(SimpleUrl.ParameterLogMaxSize, this.MaximumLogSize.ToString()));
            }
            if ((!String.IsNullOrEmpty(this.NetworkInterface)) && (String.CompareOrdinal(this.NetworkInterface, NetworkInterfaceConverter.NetworkInterfaceSystemDefault) != 0))
            {
                parameters.Add(new Parameter(SimpleUrl.ParameterNetworkInterface, this.NetworkInterface));
            }
            if ((!String.IsNullOrEmpty(this.CacheFolder)) && (String.CompareOrdinal(this.CacheFolder, CacheFolderEditor.DefaultCacheFolder) != 0))
            {
                parameters.Add(new Parameter(SimpleUrl.ParameterCacheFolder, this.CacheFolder));
            }
            if (this.DumpProtocolInputData != SimpleUrl.DefaultDumpProtocolInputData)
            {
                parameters.Add(new Parameter(SimpleUrl.ParameterDumpProtocolInputData, this.DumpProtocolInputData ? SimpleUrl.DefaultTrue : SimpleUrl.DefaultFalse));
            }
            if (this.DumpProtocolOutputData != SimpleUrl.DefaultDumpProtocolOutputData)
            {
                parameters.Add(new Parameter(SimpleUrl.ParameterDumpProtocolOutputData, this.DumpProtocolOutputData ? SimpleUrl.DefaultTrue : SimpleUrl.DefaultFalse));
            }
            if (this.DumpParserInputData != SimpleUrl.DefaultDumpParserInputData)
            {
                parameters.Add(new Parameter(SimpleUrl.ParameterDumpParserInputData, this.DumpParserInputData ? SimpleUrl.DefaultTrue : SimpleUrl.DefaultFalse));
            }
            if (this.DumpParserOutputData != SimpleUrl.DefaultDumpParserOutputData)
            {
                parameters.Add(new Parameter(SimpleUrl.ParameterDumpParserOutputData, this.DumpParserOutputData ? SimpleUrl.DefaultTrue : SimpleUrl.DefaultFalse));
            }
            if (this.DumpOutputPinData != SimpleUrl.DefaultDumpOutputPinData)
            {
                parameters.Add(new Parameter(SimpleUrl.ParameterDumpOutputPinData, this.DumpOutputPinData ? SimpleUrl.DefaultTrue : SimpleUrl.DefaultFalse));
            }

            // for MediaPortal IPTV Source Filter is live stream always true (overriden by filter itself)

            // return current URI and formatted connection string
            // filter will ignore first part

            String mpeg2Ts = this.Mpeg2TsParser.ToString();

            return this.Uri.Scheme + "://" + this.Uri.Host + SimpleUrl.ParameterSeparator + parameters.FilterParameters + ((!String.IsNullOrEmpty(mpeg2Ts)) ? ParameterCollection.ParameterSeparator : String.Empty) + ((!String.IsNullOrEmpty(mpeg2Ts)) ? mpeg2Ts : String.Empty);
        }

        /// <summary>
        /// Parses parameters from URL to current instance.
        /// </summary>
        /// <param name="parameters">The parameters from URL.</param>
        public virtual void Parse(ParameterCollection parameters)
        {
            foreach (var param in parameters)
            {
                if (String.CompareOrdinal(param.Name, SimpleUrl.ParameterNetworkInterface) == 0)
                {
                    this.NetworkInterface = param.Value;
                }

                if (String.CompareOrdinal(param.Name, SimpleUrl.ParameterUrl) == 0)
                {
                    this.Uri = new Uri(param.Value);
                }

                if (String.CompareOrdinal(param.Name, SimpleUrl.ParameterLogMaxSize) == 0)
                {
                    this.MaximumLogSize = int.Parse(param.Value);
                }

                if (String.CompareOrdinal(param.Name, SimpleUrl.ParameterLogVerbosity) == 0)
                {
                    this.Verbosity = (LogVerbosity)int.Parse(param.Value);
                }

                if (String.CompareOrdinal(param.Name, SimpleUrl.ParameterCacheFolder) == 0)
                {
                    this.CacheFolder = param.Value;
                }

                if (String.CompareOrdinal(param.Name, SimpleUrl.ParameterDumpProtocolInputData) == 0)
                {
                    this.DumpProtocolInputData = (String.CompareOrdinal(param.Value, SimpleUrl.DefaultTrue) == 0);
                }

                if (String.CompareOrdinal(param.Name, SimpleUrl.ParameterDumpProtocolOutputData) == 0)
                {
                    this.DumpProtocolOutputData = (String.CompareOrdinal(param.Value, SimpleUrl.DefaultTrue) == 0);
                }

                if (String.CompareOrdinal(param.Name, SimpleUrl.ParameterDumpParserInputData) == 0)
                {
                    this.DumpParserInputData = (String.CompareOrdinal(param.Value, SimpleUrl.DefaultTrue) == 0);
                }

                if (String.CompareOrdinal(param.Name, SimpleUrl.ParameterDumpParserOutputData) == 0)
                {
                    this.DumpParserOutputData = (String.CompareOrdinal(param.Value, SimpleUrl.DefaultTrue) == 0);
                }

                if (String.CompareOrdinal(param.Name, SimpleUrl.ParameterDumpOutputPinData) == 0)
                {
                    this.DumpOutputPinData = (String.CompareOrdinal(param.Value, SimpleUrl.DefaultTrue) == 0);
                }
            }

            this.mpeg2TsParser.Parse(parameters);
        }

        public virtual void ApplyDefaultUserSettings(ProtocolSettings previousSettings, ProtocolSettings currentSettings)
        {
            if (previousSettings == null)
            {
                throw new ArgumentNullException("previousSettings");
            }
            if (currentSettings == null)
            {
                throw new ArgumentNullException("currentSettings");
            }

            if (String.IsNullOrEmpty(this.NetworkInterface) || 
                (String.CompareOrdinal(this.NetworkInterface, NetworkInterfaceConverter.NetworkInterfaceSystemDefault) == 0) ||
                (String.CompareOrdinal(this.NetworkInterface, previousSettings.NetworkInterface) == 0))
            {
                this.NetworkInterface = currentSettings.NetworkInterface;
            }
        }

        #endregion

        #region Constants

        /// <summary>
        /// Special separator used to identify where starts parameters.
        /// </summary>
        public static readonly String ParameterSeparator = "####";

        // common parameters for MediaPortal Url Source Splitter

        /// <summary>
        /// Specifies network interface parameter name.
        /// </summary>
        protected static readonly String ParameterNetworkInterface = "Interface";

        /// <summary>
        /// Specifies URL parameter name.
        /// </summary>
        public static readonly String ParameterUrl = "Url";

        /// <summary>
        /// Specifies maximum log size parameter name.
        /// </summary>
        protected static readonly String ParameterLogMaxSize = "LogMaxSize";

        /// <summary>
        /// Specifies log verbosity parameter name.
        /// </summary>
        protected static readonly String ParameterLogVerbosity = "LogVerbosity";

        /// <summary>
        /// Specifies maximum plugins parameter name.
        /// </summary>
        protected static readonly String ParameterMaximumPlugins = "MaxPlugins";

        /// <summary>
        /// Specifies cache folder parameter name.
        /// </summary>
        protected static readonly String ParameterCacheFolder = "CacheFolder";

        /// <summary>
        /// Specifies live stream flag parameter name.
        /// </summary>
        protected static readonly String ParameterLiveStream = "LiveStream";

        /// <summary>
        /// Specifies parameter name for protocol input data.
        /// </summary>
        protected static readonly String ParameterDumpProtocolInputData = "DumpProtocolInputData";

        /// <summary>
        /// Specifies parameter name for protocol output data.
        /// </summary>
        protected static readonly String ParameterDumpProtocolOutputData = "DumpProtocolOutputData";

        /// <summary>
        /// Specifies parameter name for parser input data.
        /// </summary>
        protected static readonly String ParameterDumpParserInputData = "DumpParserInputData";

        /// <summary>
        /// Specifies parameter name for parser output data.
        /// </summary>
        protected static readonly String ParameterDumpParserOutputData = "DumpParserOutputData";

        /// <summary>
        /// Specifies parameter name for output pin data.
        /// </summary>
        protected static readonly String ParameterDumpOutputPinData = "DumpOutputPinData";

        // default values for some parameters

        /// <summary>
        /// Default verbosity.
        /// </summary>
        /// <remarks>
        /// The default value is <see cref="LogVerbosity.Verbose"/>.
        /// </remarks>
        public const LogVerbosity DefaultVerbosity = LogVerbosity.Verbose;

        /// <summary>
        /// Default maximum log size.
        /// </summary>
        /// <remarks>
        /// The default values is 10 MB.
        /// </remarks>
        public const int DefaultLogMaximumSize = 10 * 1024 * 1024;

        /// <summary>
        /// Default maximum plugins.
        /// </summary>
        /// <remarks>
        /// The default value is 256.
        /// </remarks>
        public const int DefaultMaximumPlugins = 256;

        /// <summary>
        /// Default value of live stream flag.
        /// </summary>
        /// <remarks>
        /// The default value is <see langword="false"/>.
        /// </remarks>
        public const Boolean DefaultLiveStream = false;

        /// <summary>
        /// Specifies <see langword="true"/> value for filter parameter value.
        /// </summary>
        public static readonly String DefaultTrue = "1";

        /// <summary>
        /// Specifies <see langword="false"/> value for filter parameter value.
        /// </summary>
        public static readonly String DefaultFalse = "0";

        /// <summary>
        /// Default value for parameter for protocol input data parameter.
        /// </summary>
        public const Boolean DefaultDumpProtocolInputData = false;

        /// <summary>
        /// Default value for protocol output data parameter.
        /// </summary>
        public const Boolean DefaultDumpProtocolOutputData = false;

        /// <summary>
        /// Default value for parser input data parameter.
        /// </summary>
        public const Boolean DefaultDumpParserInputData = false;

        /// <summary>
        /// Default value for parser output data parameter.
        /// </summary>
        public const Boolean DefaultDumpParserOutputData = false;

        /// <summary>
        /// Default value for output pin data parameter.
        /// </summary>
        public const Boolean DefaultDumpOutputPinData = false;

        #endregion
    }
}
