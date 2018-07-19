using System;
using System.Collections.Generic;
using Java.Lang;
using Java.Net;
using Java.Util;
using Javax.Servlet;
using Javax.Servlet.Http;
using Sharpen;

namespace Edu.Stanford.Nlp.Naturalli.Demo
{
	/// <summary>
	/// <p>
	/// A
	/// <see cref="Javax.Servlet.IFilter"/>
	/// that enable client-side cross-origin requests by
	/// implementing W3C's CORS (<b>C</b>ross-<b>O</b>rigin <b>R</b>esource
	/// <b>S</b>haring) specification for resources. Each
	/// <see cref="Javax.Servlet.Http.IHttpServletRequest"/>
	/// request is inspected as per specification, and appropriate response headers
	/// are added to
	/// <see cref="Javax.Servlet.Http.IHttpServletResponse"/>
	/// .
	/// </p>
	/// <p>
	/// By default, it also sets following request attributes, that helps to
	/// determine nature of request downstream.
	/// <ul>
	/// <li><b>cors.isCorsRequest:</b> Flag to determine if request is a CORS
	/// request. Set to <code>true</code> if CORS request; <code>false</code>
	/// otherwise.</li>
	/// <li><b>cors.request.origin:</b> The Origin URL.</li>
	/// <li><b>cors.request.type:</b> Type of request. Values: <code>simple</code> or
	/// <code>preflight</code> or <code>not_cors</code> or <code>invalid_cors</code></li>
	/// <li><b>cors.request.headers:</b> Request headers sent as
	/// 'Access-Control-Request-Headers' header, for pre-flight request.</li>
	/// </ul>
	/// </p>
	/// </summary>
	/// <author>Mohit Soni</author>
	/// <seealso>
	/// <a href="http://www.w3.org/TR/cors/">CORS specification</a>
	/// note[Gabor]: STOLEN SHAMELESSLY FROM https://github.com/eBay/cors-filter/blob/master/LICENSE
	/// DO NOT DISTRIBUTE WITH CORENLP!!!!
	/// </seealso>
	public sealed class CORSFilter : IFilter
	{
		/// <summary>Holds filter configuration.</summary>
		private IFilterConfig filterConfig;

		/// <summary>
		/// A
		/// <see cref="System.Collections.ICollection{E}"/>
		/// of origins consisting of zero or more origins that
		/// are allowed access to the resource.
		/// </summary>
		private readonly ICollection<string> allowedOrigins;

		/// <summary>Determines if any origin is allowed to make request.</summary>
		private bool anyOriginAllowed;

		/// <summary>
		/// A
		/// <see cref="System.Collections.ICollection{E}"/>
		/// of methods consisting of zero or more methods that
		/// are supported by the resource.
		/// </summary>
		private readonly ICollection<string> allowedHttpMethods;

		/// <summary>
		/// A
		/// <see cref="System.Collections.ICollection{E}"/>
		/// of headers consisting of zero or more header field
		/// names that are supported by the resource.
		/// </summary>
		private readonly ICollection<string> allowedHttpHeaders;

		/// <summary>
		/// A
		/// <see cref="System.Collections.ICollection{E}"/>
		/// of exposed headers consisting of zero or more header
		/// field names of headers other than the simple response headers that the
		/// resource might use and can be exposed.
		/// </summary>
		private readonly ICollection<string> exposedHeaders;

		/// <summary>
		/// A supports credentials flag that indicates whether the resource supports
		/// user credentials in the request.
		/// </summary>
		/// <remarks>
		/// A supports credentials flag that indicates whether the resource supports
		/// user credentials in the request. It is true when the resource does and
		/// false otherwise.
		/// </remarks>
		private bool supportsCredentials;

		/// <summary>
		/// Indicates (in seconds) how long the results of a pre-flight request can
		/// be cached in a pre-flight result cache.
		/// </summary>
		private long preflightMaxAge;

		/// <summary>Controls access log logging.</summary>
		private bool loggingEnabled;

		/// <summary>Determines if the request should be decorated or not.</summary>
		private bool decorateRequest;

		public CORSFilter()
		{
			// ----------------------------------------------------- Instance variables
			// --------------------------------------------------------- Constructor(s)
			this.allowedOrigins = new HashSet<string>();
			this.allowedHttpMethods = new HashSet<string>();
			this.allowedHttpHeaders = new HashSet<string>();
			this.exposedHeaders = new HashSet<string>();
		}

		// --------------------------------------------------------- Public methods
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="Javax.Servlet.ServletException"/>
		public void DoFilter(IServletRequest servletRequest, IServletResponse servletResponse, IFilterChain filterChain)
		{
			if (!(servletRequest is IHttpServletRequest) || !(servletResponse is IHttpServletResponse))
			{
				string message = "CORS doesn't support non-HTTP request or response.";
				throw new ServletException(message);
			}
			// Safe to downcast at this point.
			IHttpServletRequest request = (IHttpServletRequest)servletRequest;
			IHttpServletResponse response = (IHttpServletResponse)servletResponse;
			// Determines the CORS request type.
			CORSFilter.CORSRequestType requestType = CheckRequestType(request);
			// Adds CORS specific attributes to request.
			if (decorateRequest)
			{
				Edu.Stanford.Nlp.Naturalli.Demo.CORSFilter.DecorateCORSProperties(request, requestType);
			}
			switch (requestType)
			{
				case CORSFilter.CORSRequestType.Simple:
				{
					// Handles a Simple CORS request.
					this.HandleSimpleCORS(request, response, filterChain);
					break;
				}

				case CORSFilter.CORSRequestType.Actual:
				{
					// Handles an Actual CORS request.
					this.HandleSimpleCORS(request, response, filterChain);
					break;
				}

				case CORSFilter.CORSRequestType.PreFlight:
				{
					// Handles a Pre-flight CORS request.
					this.HandlePreflightCORS(request, response, filterChain);
					break;
				}

				case CORSFilter.CORSRequestType.NotCors:
				{
					// Handles a Normal request that is not a cross-origin request.
					this.HandleNonCORS(request, response, filterChain);
					break;
				}

				default:
				{
					// Handles a CORS request that violates specification.
					this.HandleInvalidCORS(request, response, filterChain);
					break;
				}
			}
		}

		/// <exception cref="Javax.Servlet.ServletException"/>
		public void Init(IFilterConfig filterConfig)
		{
			// Initialize defaults
			ParseAndStore(DefaultAllowedOrigins, DefaultAllowedHttpMethods, DefaultAllowedHttpHeaders, DefaultExposedHeaders, DefaultSupportsCredentials, DefaultPreflightMaxage, DefaultLoggingEnabled, DefaultDecorateRequest);
			this.filterConfig = filterConfig;
			this.loggingEnabled = false;
			if (filterConfig != null)
			{
				string configAllowedOrigins = filterConfig.GetInitParameter(ParamCorsAllowedOrigins);
				string configAllowedHttpMethods = filterConfig.GetInitParameter(ParamCorsAllowedMethods);
				string configAllowedHttpHeaders = filterConfig.GetInitParameter(ParamCorsAllowedHeaders);
				string configExposedHeaders = filterConfig.GetInitParameter(ParamCorsExposedHeaders);
				string configSupportsCredentials = filterConfig.GetInitParameter(ParamCorsSupportCredentials);
				string configPreflightMaxAge = filterConfig.GetInitParameter(ParamCorsPreflightMaxage);
				string configLoggingEnabled = filterConfig.GetInitParameter(ParamCorsLoggingEnabled);
				string configDecorateRequest = filterConfig.GetInitParameter(ParamCorsRequestDecorate);
				ParseAndStore(configAllowedOrigins, configAllowedHttpMethods, configAllowedHttpHeaders, configExposedHeaders, configSupportsCredentials, configPreflightMaxAge, configLoggingEnabled, configDecorateRequest);
			}
		}

		// --------------------------------------------------------------- Handlers
		/// <summary>
		/// Handles a CORS request of type
		/// <see cref="CORSRequestType"/>
		/// .SIMPLE.
		/// </summary>
		/// <param name="request">
		/// The
		/// <see cref="Javax.Servlet.Http.IHttpServletRequest"/>
		/// object.
		/// </param>
		/// <param name="response">
		/// The
		/// <see cref="Javax.Servlet.Http.IHttpServletResponse"/>
		/// object.
		/// </param>
		/// <param name="filterChain">
		/// The
		/// <see cref="Javax.Servlet.IFilterChain"/>
		/// object.
		/// </param>
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="Javax.Servlet.ServletException"/>
		/// <seealso><a href="http://www.w3.org/TR/cors/#resource-requests">Simple
		/// *      Cross-Origin Request, Actual Request, and Redirects</a></seealso>
		public void HandleSimpleCORS(IHttpServletRequest request, IHttpServletResponse response, IFilterChain filterChain)
		{
			CORSFilter.CORSRequestType requestType = CheckRequestType(request);
			if (!(requestType == CORSFilter.CORSRequestType.Simple || requestType == CORSFilter.CORSRequestType.Actual))
			{
				string message = "Expects a HttpServletRequest object of type " + CORSFilter.CORSRequestType.Simple + " or " + CORSFilter.CORSRequestType.Actual;
				throw new ArgumentException(message);
			}
			string origin = request.GetHeader(Edu.Stanford.Nlp.Naturalli.Demo.CORSFilter.RequestHeaderOrigin);
			string method = request.GetMethod();
			// Section 6.1.2
			if (!IsOriginAllowed(origin))
			{
				HandleInvalidCORS(request, response, filterChain);
				return;
			}
			if (!allowedHttpMethods.Contains(method))
			{
				HandleInvalidCORS(request, response, filterChain);
				return;
			}
			// Section 6.1.3
			// Add a single Access-Control-Allow-Origin header.
			if (anyOriginAllowed && !supportsCredentials)
			{
				// If resource doesn't support credentials and if any origin is
				// allowed
				// to make CORS request, return header with '*'.
				response.AddHeader(Edu.Stanford.Nlp.Naturalli.Demo.CORSFilter.ResponseHeaderAccessControlAllowOrigin, "*");
			}
			else
			{
				// If the resource supports credentials add a single
				// Access-Control-Allow-Origin header, with the value of the Origin
				// header as value.
				response.AddHeader(Edu.Stanford.Nlp.Naturalli.Demo.CORSFilter.ResponseHeaderAccessControlAllowOrigin, origin);
			}
			// Section 6.1.3
			// If the resource supports credentials, add a single
			// Access-Control-Allow-Credentials header with the case-sensitive
			// string "true" as value.
			if (supportsCredentials)
			{
				response.AddHeader(Edu.Stanford.Nlp.Naturalli.Demo.CORSFilter.ResponseHeaderAccessControlAllowCredentials, "true");
			}
			// Section 6.1.4
			// If the list of exposed headers is not empty add one or more
			// Access-Control-Expose-Headers headers, with as values the header
			// field names given in the list of exposed headers.
			if ((exposedHeaders != null) && (exposedHeaders.Count > 0))
			{
				string exposedHeadersString = Join(exposedHeaders, ",");
				response.AddHeader(Edu.Stanford.Nlp.Naturalli.Demo.CORSFilter.ResponseHeaderAccessControlExposeHeaders, exposedHeadersString);
			}
			// Forward the request down the filter chain.
			filterChain.DoFilter(request, response);
		}

		/// <summary>Handles CORS pre-flight request.</summary>
		/// <param name="request">
		/// The
		/// <see cref="Javax.Servlet.Http.IHttpServletRequest"/>
		/// object.
		/// </param>
		/// <param name="response">
		/// The
		/// <see cref="Javax.Servlet.Http.IHttpServletResponse"/>
		/// object.
		/// </param>
		/// <param name="filterChain">
		/// The
		/// <see cref="Javax.Servlet.IFilterChain"/>
		/// object.
		/// </param>
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="Javax.Servlet.ServletException"/>
		public void HandlePreflightCORS(IHttpServletRequest request, IHttpServletResponse response, IFilterChain filterChain)
		{
			CORSFilter.CORSRequestType requestType = CheckRequestType(request);
			if (requestType != CORSFilter.CORSRequestType.PreFlight)
			{
				throw new ArgumentException("Expects a HttpServletRequest object of type " + CORSFilter.CORSRequestType.PreFlight.ToString().ToLower());
			}
			string origin = request.GetHeader(Edu.Stanford.Nlp.Naturalli.Demo.CORSFilter.RequestHeaderOrigin);
			// Section 6.2.2
			if (!IsOriginAllowed(origin))
			{
				HandleInvalidCORS(request, response, filterChain);
				return;
			}
			// Section 6.2.3
			string accessControlRequestMethod = request.GetHeader(Edu.Stanford.Nlp.Naturalli.Demo.CORSFilter.RequestHeaderAccessControlRequestMethod);
			if (accessControlRequestMethod == null || (!HttpMethods.Contains(accessControlRequestMethod.Trim())))
			{
				HandleInvalidCORS(request, response, filterChain);
				return;
			}
			else
			{
				accessControlRequestMethod = accessControlRequestMethod.Trim();
			}
			// Section 6.2.4
			string accessControlRequestHeadersHeader = request.GetHeader(Edu.Stanford.Nlp.Naturalli.Demo.CORSFilter.RequestHeaderAccessControlRequestHeaders);
			IList<string> accessControlRequestHeaders = new LinkedList<string>();
			if (accessControlRequestHeadersHeader != null && !accessControlRequestHeadersHeader.Trim().IsEmpty())
			{
				string[] headers = accessControlRequestHeadersHeader.Trim().Split(",");
				foreach (string header in headers)
				{
					accessControlRequestHeaders.Add(header.Trim().ToLower());
				}
			}
			// Section 6.2.5
			if (!allowedHttpMethods.Contains(accessControlRequestMethod))
			{
				HandleInvalidCORS(request, response, filterChain);
				return;
			}
			// Section 6.2.6
			if (!accessControlRequestHeaders.IsEmpty())
			{
				foreach (string header in accessControlRequestHeaders)
				{
					if (!allowedHttpHeaders.Contains(header))
					{
						HandleInvalidCORS(request, response, filterChain);
						return;
					}
				}
			}
			// Section 6.2.7
			if (supportsCredentials)
			{
				response.AddHeader(Edu.Stanford.Nlp.Naturalli.Demo.CORSFilter.ResponseHeaderAccessControlAllowOrigin, origin);
				response.AddHeader(Edu.Stanford.Nlp.Naturalli.Demo.CORSFilter.ResponseHeaderAccessControlAllowCredentials, "true");
			}
			else
			{
				if (anyOriginAllowed)
				{
					response.AddHeader(Edu.Stanford.Nlp.Naturalli.Demo.CORSFilter.ResponseHeaderAccessControlAllowOrigin, "*");
				}
				else
				{
					response.AddHeader(Edu.Stanford.Nlp.Naturalli.Demo.CORSFilter.ResponseHeaderAccessControlAllowOrigin, origin);
				}
			}
			// Section 6.2.8
			if (preflightMaxAge > 0)
			{
				response.AddHeader(Edu.Stanford.Nlp.Naturalli.Demo.CORSFilter.ResponseHeaderAccessControlMaxAge, preflightMaxAge.ToString());
			}
			// Section 6.2.9
			response.AddHeader(Edu.Stanford.Nlp.Naturalli.Demo.CORSFilter.ResponseHeaderAccessControlAllowMethods, accessControlRequestMethod);
			// Section 6.2.10
			if ((allowedHttpHeaders != null) && (!allowedHttpHeaders.IsEmpty()))
			{
				response.AddHeader(Edu.Stanford.Nlp.Naturalli.Demo.CORSFilter.ResponseHeaderAccessControlAllowHeaders, Join(allowedHttpHeaders, ","));
			}
		}

		// Do not forward the request down the filter chain.
		/// <summary>Handles a request, that's not a CORS request, but is a valid request i.e.</summary>
		/// <remarks>
		/// Handles a request, that's not a CORS request, but is a valid request i.e.
		/// it is not a cross-origin request. This implementation, just forwards the
		/// request down the filter chain.
		/// </remarks>
		/// <param name="request">
		/// The
		/// <see cref="Javax.Servlet.Http.IHttpServletRequest"/>
		/// object.
		/// </param>
		/// <param name="response">
		/// The
		/// <see cref="Javax.Servlet.Http.IHttpServletResponse"/>
		/// object.
		/// </param>
		/// <param name="filterChain">
		/// The
		/// <see cref="Javax.Servlet.IFilterChain"/>
		/// object.
		/// </param>
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="Javax.Servlet.ServletException"/>
		public void HandleNonCORS(IHttpServletRequest request, IHttpServletResponse response, IFilterChain filterChain)
		{
			// Let request pass.
			filterChain.DoFilter(request, response);
		}

		/// <summary>Handles a CORS request that violates specification.</summary>
		/// <param name="request">
		/// The
		/// <see cref="Javax.Servlet.Http.IHttpServletRequest"/>
		/// object.
		/// </param>
		/// <param name="response">
		/// The
		/// <see cref="Javax.Servlet.Http.IHttpServletResponse"/>
		/// object.
		/// </param>
		/// <param name="filterChain">
		/// The
		/// <see cref="Javax.Servlet.IFilterChain"/>
		/// object.
		/// </param>
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="Javax.Servlet.ServletException"/>
		public void HandleInvalidCORS(IHttpServletRequest request, IHttpServletResponse response, IFilterChain filterChain)
		{
			string origin = request.GetHeader(Edu.Stanford.Nlp.Naturalli.Demo.CORSFilter.RequestHeaderOrigin);
			string method = request.GetMethod();
			string accessControlRequestHeaders = request.GetHeader(RequestHeaderAccessControlRequestHeaders);
			string message = "Invalid CORS request; Origin=" + origin + ";Method=" + method;
			if (accessControlRequestHeaders != null)
			{
				message = message + ";Access-Control-Request-Headers=" + accessControlRequestHeaders;
			}
			response.SetContentType("text/plain");
			response.SetStatus(HttpServletResponseConstants.ScForbidden);
			response.ResetBuffer();
			Log(message);
		}

		public void Destroy()
		{
		}

		// NOOP
		// -------------------------------------------------------- Utility methods
		/// <summary>
		/// Decorates the
		/// <see cref="Javax.Servlet.Http.IHttpServletRequest"/>
		/// , with CORS attributes.
		/// <ul>
		/// <li><b>cors.isCorsRequest:</b> Flag to determine if request is a CORS
		/// request. Set to <code>true</code> if CORS request; <code>false</code>
		/// otherwise.</li>
		/// <li><b>cors.request.origin:</b> The Origin URL.</li>
		/// <li><b>cors.request.type:</b> Type of request. Values:
		/// <code>simple</code> or <code>preflight</code> or <code>not_cors</code> or
		/// <code>invalid_cors</code></li>
		/// <li><b>cors.request.headers:</b> Request headers sent as
		/// 'Access-Control-Request-Headers' header, for pre-flight request.</li>
		/// </ul>
		/// </summary>
		/// <param name="request">
		/// The
		/// <see cref="Javax.Servlet.Http.IHttpServletRequest"/>
		/// object.
		/// </param>
		/// <param name="corsRequestType">
		/// The
		/// <see cref="CORSRequestType"/>
		/// object.
		/// </param>
		public static void DecorateCORSProperties(IHttpServletRequest request, CORSFilter.CORSRequestType corsRequestType)
		{
			if (request == null)
			{
				throw new ArgumentException("HttpServletRequest object is null");
			}
			if (corsRequestType == null)
			{
				throw new ArgumentException("CORSRequestType object is null");
			}
			switch (corsRequestType)
			{
				case CORSFilter.CORSRequestType.Simple:
				{
					request.SetAttribute(Edu.Stanford.Nlp.Naturalli.Demo.CORSFilter.HttpRequestAttributeIsCorsRequest, true);
					request.SetAttribute(Edu.Stanford.Nlp.Naturalli.Demo.CORSFilter.HttpRequestAttributeOrigin, request.GetHeader(Edu.Stanford.Nlp.Naturalli.Demo.CORSFilter.RequestHeaderOrigin));
					request.SetAttribute(Edu.Stanford.Nlp.Naturalli.Demo.CORSFilter.HttpRequestAttributeRequestType, corsRequestType.ToString().ToLower());
					break;
				}

				case CORSFilter.CORSRequestType.Actual:
				{
					request.SetAttribute(Edu.Stanford.Nlp.Naturalli.Demo.CORSFilter.HttpRequestAttributeIsCorsRequest, true);
					request.SetAttribute(Edu.Stanford.Nlp.Naturalli.Demo.CORSFilter.HttpRequestAttributeOrigin, request.GetHeader(Edu.Stanford.Nlp.Naturalli.Demo.CORSFilter.RequestHeaderOrigin));
					request.SetAttribute(Edu.Stanford.Nlp.Naturalli.Demo.CORSFilter.HttpRequestAttributeRequestType, corsRequestType.ToString().ToLower());
					break;
				}

				case CORSFilter.CORSRequestType.PreFlight:
				{
					request.SetAttribute(Edu.Stanford.Nlp.Naturalli.Demo.CORSFilter.HttpRequestAttributeIsCorsRequest, true);
					request.SetAttribute(Edu.Stanford.Nlp.Naturalli.Demo.CORSFilter.HttpRequestAttributeOrigin, request.GetHeader(Edu.Stanford.Nlp.Naturalli.Demo.CORSFilter.RequestHeaderOrigin));
					request.SetAttribute(Edu.Stanford.Nlp.Naturalli.Demo.CORSFilter.HttpRequestAttributeRequestType, corsRequestType.ToString().ToLower());
					string headers = request.GetHeader(RequestHeaderAccessControlRequestHeaders);
					if (headers == null)
					{
						headers = string.Empty;
					}
					request.SetAttribute(Edu.Stanford.Nlp.Naturalli.Demo.CORSFilter.HttpRequestAttributeRequestHeaders, headers);
					break;
				}

				case CORSFilter.CORSRequestType.NotCors:
				{
					request.SetAttribute(Edu.Stanford.Nlp.Naturalli.Demo.CORSFilter.HttpRequestAttributeIsCorsRequest, false);
					break;
				}

				default:
				{
					// Don't set any attributes
					break;
				}
			}
		}

		/// <summary>
		/// Joins elements of
		/// <see cref="Java.Util.ISet{E}"/>
		/// into a string, where each element is
		/// separated by the provided separator.
		/// </summary>
		/// <param name="elements">
		/// The
		/// <see cref="Java.Util.ISet{E}"/>
		/// containing elements to join together.
		/// </param>
		/// <param name="joinSeparator">The character to be used for separating elements.</param>
		/// <returns>
		/// The joined
		/// <see cref="string"/>
		/// ; <code>null</code> if elements
		/// <see cref="Java.Util.ISet{E}"/>
		/// is null.
		/// </returns>
		public static string Join(ICollection<string> elements, string joinSeparator)
		{
			string separator = ",";
			if (elements == null)
			{
				return null;
			}
			if (joinSeparator != null)
			{
				separator = joinSeparator;
			}
			StringBuilder buffer = new StringBuilder();
			bool isFirst = true;
			foreach (string element in elements)
			{
				if (!isFirst)
				{
					buffer.Append(separator);
				}
				else
				{
					isFirst = false;
				}
				if (element != null)
				{
					buffer.Append(element);
				}
			}
			return buffer.ToString();
		}

		/// <summary>Determines the request type.</summary>
		/// <param name="request"/>
		/// <returns/>
		public CORSFilter.CORSRequestType CheckRequestType(IHttpServletRequest request)
		{
			CORSFilter.CORSRequestType requestType = CORSFilter.CORSRequestType.InvalidCors;
			if (request == null)
			{
				throw new ArgumentException("HttpServletRequest object is null");
			}
			string originHeader = request.GetHeader(RequestHeaderOrigin);
			// Section 6.1.1 and Section 6.2.1
			if (originHeader != null)
			{
				if (originHeader.IsEmpty())
				{
					requestType = CORSFilter.CORSRequestType.InvalidCors;
				}
				else
				{
					if (!IsValidOrigin(originHeader))
					{
						requestType = CORSFilter.CORSRequestType.InvalidCors;
					}
					else
					{
						string method = request.GetMethod();
						if (method != null && HttpMethods.Contains(method))
						{
							if ("OPTIONS".Equals(method))
							{
								string accessControlRequestMethodHeader = request.GetHeader(RequestHeaderAccessControlRequestMethod);
								if (accessControlRequestMethodHeader != null && !accessControlRequestMethodHeader.IsEmpty())
								{
									requestType = CORSFilter.CORSRequestType.PreFlight;
								}
								else
								{
									if (accessControlRequestMethodHeader != null && accessControlRequestMethodHeader.IsEmpty())
									{
										requestType = CORSFilter.CORSRequestType.InvalidCors;
									}
									else
									{
										requestType = CORSFilter.CORSRequestType.Actual;
									}
								}
							}
							else
							{
								if ("GET".Equals(method) || "HEAD".Equals(method))
								{
									requestType = CORSFilter.CORSRequestType.Simple;
								}
								else
								{
									if ("POST".Equals(method))
									{
										string contentType = request.GetContentType();
										if (contentType != null)
										{
											contentType = contentType.ToLower().Trim();
											if (SimpleHttpRequestContentTypeValues.Contains(contentType))
											{
												requestType = CORSFilter.CORSRequestType.Simple;
											}
											else
											{
												requestType = CORSFilter.CORSRequestType.Actual;
											}
										}
									}
									else
									{
										if (ComplexHttpMethods.Contains(method))
										{
											requestType = CORSFilter.CORSRequestType.Actual;
										}
									}
								}
							}
						}
					}
				}
			}
			else
			{
				requestType = CORSFilter.CORSRequestType.NotCors;
			}
			return requestType;
		}

		/// <summary>Checks if the Origin is allowed to make a CORS request.</summary>
		/// <param name="origin">The Origin.</param>
		/// <returns>
		/// <code>true</code> if origin is allowed; <code>false</code>
		/// otherwise.
		/// </returns>
		private bool IsOriginAllowed(string origin)
		{
			if (anyOriginAllowed)
			{
				return true;
			}
			// If 'Origin' header is a case-sensitive match of any of allowed
			// origins, then return true, else return false.
			return allowedOrigins.Contains(origin);
		}

		private void Log(string message)
		{
			if (loggingEnabled)
			{
				filterConfig.GetServletContext().Log(message);
			}
		}

		/// <summary>Parses each param-value and populates configuration variables.</summary>
		/// <remarks>
		/// Parses each param-value and populates configuration variables. If a param
		/// is provided, it overrides the default.
		/// </remarks>
		/// <param name="allowedOrigins">
		/// A
		/// <see cref="string"/>
		/// of comma separated origins.
		/// </param>
		/// <param name="allowedHttpMethods">
		/// A
		/// <see cref="string"/>
		/// of comma separated HTTP methods.
		/// </param>
		/// <param name="allowedHttpHeaders">
		/// A
		/// <see cref="string"/>
		/// of comma separated HTTP headers.
		/// </param>
		/// <param name="exposedHeaders">
		/// A
		/// <see cref="string"/>
		/// of comma separated headers that needs to be
		/// exposed.
		/// </param>
		/// <param name="supportsCredentials">"true" if support credentials needs to be enabled.</param>
		/// <param name="preflightMaxAge">
		/// The amount of seconds the user agent is allowed to cache the
		/// result of the pre-flight request.
		/// </param>
		/// <param name="loggingEnabled">Flag to control logging to access log.</param>
		/// <exception cref="Javax.Servlet.ServletException"/>
		private void ParseAndStore(string allowedOrigins, string allowedHttpMethods, string allowedHttpHeaders, string exposedHeaders, string supportsCredentials, string preflightMaxAge, string loggingEnabled, string decorateRequest)
		{
			if (allowedOrigins != null)
			{
				if (allowedOrigins.Trim().Equals("*"))
				{
					this.anyOriginAllowed = true;
				}
				else
				{
					this.anyOriginAllowed = false;
					ICollection<string> setAllowedOrigins = ParseStringToSet(allowedOrigins);
					this.allowedOrigins.Clear();
					Sharpen.Collections.AddAll(this.allowedOrigins, setAllowedOrigins);
				}
			}
			if (allowedHttpMethods != null)
			{
				ICollection<string> setAllowedHttpMethods = ParseStringToSet(allowedHttpMethods);
				this.allowedHttpMethods.Clear();
				Sharpen.Collections.AddAll(this.allowedHttpMethods, setAllowedHttpMethods);
			}
			if (allowedHttpHeaders != null)
			{
				ICollection<string> setAllowedHttpHeaders = ParseStringToSet(allowedHttpHeaders);
				ICollection<string> lowerCaseHeaders = new HashSet<string>();
				foreach (string header in setAllowedHttpHeaders)
				{
					string lowerCase = header.ToLower();
					lowerCaseHeaders.Add(lowerCase);
				}
				this.allowedHttpHeaders.Clear();
				Sharpen.Collections.AddAll(this.allowedHttpHeaders, lowerCaseHeaders);
			}
			if (exposedHeaders != null)
			{
				ICollection<string> setExposedHeaders = ParseStringToSet(exposedHeaders);
				this.exposedHeaders.Clear();
				Sharpen.Collections.AddAll(this.exposedHeaders, setExposedHeaders);
			}
			if (supportsCredentials != null)
			{
				// For any value other then 'true' this will be false.
				this.supportsCredentials = bool.ParseBoolean(supportsCredentials);
			}
			if (preflightMaxAge != null)
			{
				try
				{
					if (!preflightMaxAge.IsEmpty())
					{
						this.preflightMaxAge = long.Parse(preflightMaxAge);
					}
					else
					{
						this.preflightMaxAge = 0L;
					}
				}
				catch (NumberFormatException e)
				{
					throw new ServletException("Unable to parse preflightMaxAge", e);
				}
			}
			if (loggingEnabled != null)
			{
				// For any value other then 'true' this will be false.
				this.loggingEnabled = bool.ParseBoolean(loggingEnabled);
			}
			if (decorateRequest != null)
			{
				// For any value other then 'true' this will be false.
				this.decorateRequest = bool.ParseBoolean(decorateRequest);
			}
		}

		/// <summary>Takes a comma separated list and returns a Set<String>.</summary>
		/// <param name="data">A comma separated list of strings.</param>
		/// <returns>Set<String></returns>
		private ICollection<string> ParseStringToSet(string data)
		{
			string[] splits;
			if (data != null && data.Length > 0)
			{
				splits = data.Split(",");
			}
			else
			{
				splits = new string[] {  };
			}
			ICollection<string> set = new HashSet<string>();
			if (splits.Length > 0)
			{
				foreach (string split in splits)
				{
					set.Add(split.Trim());
				}
			}
			return set;
		}

		/// <summary>Checks if a given origin is valid or not.</summary>
		/// <remarks>
		/// Checks if a given origin is valid or not. Criteria:
		/// <ul>
		/// <li>If an encoded character is present in origin, it's not valid.</li>
		/// <li>Origin should be a valid
		/// <see cref="Java.Net.URI"/>
		/// </li>
		/// </ul>
		/// </remarks>
		/// <param name="origin"/>
		/// <seealso><a href="http://tools.ietf.org/html/rfc952">RFC952</a></seealso>
		/// <returns/>
		public static bool IsValidOrigin(string origin)
		{
			// Checks for encoded characters. Helps prevent CRLF injection.
			if (origin.Contains("%"))
			{
				return false;
			}
			URI originURI;
			try
			{
				originURI = new URI(origin);
			}
			catch (URISyntaxException)
			{
				return false;
			}
			// If scheme for URI is null, return false. Return true otherwise.
			return originURI.GetScheme() != null;
		}

		// -------------------------------------------------------------- Accessors
		/// <summary>Determines if logging is enabled or not.</summary>
		/// <returns><code>true</code> if it's enabled; false otherwise.</returns>
		public bool IsLoggingEnabled()
		{
			return loggingEnabled;
		}

		/// <summary>Determines if any origin is allowed to make CORS request.</summary>
		/// <returns><code>true</code> if it's enabled; false otherwise.</returns>
		public bool IsAnyOriginAllowed()
		{
			return anyOriginAllowed;
		}

		/// <summary>
		/// Returns a
		/// <see cref="Java.Util.ISet{E}"/>
		/// of headers that should be exposed by browser.
		/// </summary>
		/// <returns/>
		public ICollection<string> GetExposedHeaders()
		{
			return exposedHeaders;
		}

		/// <summary>Determines is supports credentials is enabled</summary>
		/// <returns/>
		public bool IsSupportsCredentials()
		{
			return supportsCredentials;
		}

		/// <summary>Returns the preflight response cache time in seconds.</summary>
		/// <returns>Time to cache in seconds.</returns>
		public long GetPreflightMaxAge()
		{
			return preflightMaxAge;
		}

		/// <summary>
		/// Returns the
		/// <see cref="Java.Util.ISet{E}"/>
		/// of allowed origins that are allowed to make
		/// requests.
		/// </summary>
		/// <returns>
		/// 
		/// <see cref="Java.Util.ISet{E}"/>
		/// </returns>
		public ICollection<string> GetAllowedOrigins()
		{
			return allowedOrigins;
		}

		/// <summary>
		/// Returns a
		/// <see cref="Java.Util.ISet{E}"/>
		/// of HTTP methods that are allowed to make requests.
		/// </summary>
		/// <returns>
		/// 
		/// <see cref="Java.Util.ISet{E}"/>
		/// </returns>
		public ICollection<string> GetAllowedHttpMethods()
		{
			return allowedHttpMethods;
		}

		/// <summary>
		/// Returns a
		/// <see cref="Java.Util.ISet{E}"/>
		/// of headers support by resource.
		/// </summary>
		/// <returns>
		/// 
		/// <see cref="Java.Util.ISet{E}"/>
		/// </returns>
		public ICollection<string> GetAllowedHttpHeaders()
		{
			return allowedHttpHeaders;
		}

		/// <summary>
		/// The Access-Control-Allow-Origin header indicates whether a resource can
		/// be shared based by returning the value of the Origin request header in
		/// the response.
		/// </summary>
		public const string ResponseHeaderAccessControlAllowOrigin = "Access-Control-Allow-Origin";

		/// <summary>
		/// The Access-Control-Allow-Credentials header indicates whether the
		/// response to request can be exposed when the omit credentials flag is
		/// unset.
		/// </summary>
		/// <remarks>
		/// The Access-Control-Allow-Credentials header indicates whether the
		/// response to request can be exposed when the omit credentials flag is
		/// unset. When part of the response to a preflight request it indicates that
		/// the actual request can include user credentials.
		/// </remarks>
		public const string ResponseHeaderAccessControlAllowCredentials = "Access-Control-Allow-Credentials";

		/// <summary>
		/// The Access-Control-Expose-Headers header indicates which headers are safe
		/// to expose to the API of a CORS API specification
		/// </summary>
		public const string ResponseHeaderAccessControlExposeHeaders = "Access-Control-Expose-Headers";

		/// <summary>
		/// The Access-Control-Max-Age header indicates how long the results of a
		/// preflight request can be cached in a preflight result cache.
		/// </summary>
		public const string ResponseHeaderAccessControlMaxAge = "Access-Control-Max-Age";

		/// <summary>
		/// The Access-Control-Allow-Methods header indicates, as part of the
		/// response to a preflight request, which methods can be used during the
		/// actual request.
		/// </summary>
		public const string ResponseHeaderAccessControlAllowMethods = "Access-Control-Allow-Methods";

		/// <summary>
		/// The Access-Control-Allow-Headers header indicates, as part of the
		/// response to a preflight request, which header field names can be used
		/// during the actual request.
		/// </summary>
		public const string ResponseHeaderAccessControlAllowHeaders = "Access-Control-Allow-Headers";

		/// <summary>
		/// The Origin header indicates where the cross-origin request or preflight
		/// request originates from.
		/// </summary>
		public const string RequestHeaderOrigin = "Origin";

		/// <summary>
		/// The Access-Control-Request-Method header indicates which method will be
		/// used in the actual request as part of the preflight request.
		/// </summary>
		public const string RequestHeaderAccessControlRequestMethod = "Access-Control-Request-Method";

		/// <summary>
		/// The Access-Control-Request-Headers header indicates which headers will be
		/// used in the actual request as part of the preflight request.
		/// </summary>
		public const string RequestHeaderAccessControlRequestHeaders = "Access-Control-Request-Headers";

		/// <summary>The prefix to a CORS request attribute.</summary>
		public const string HttpRequestAttributePrefix = "cors.";

		/// <summary>Attribute that contains the origin of the request.</summary>
		public const string HttpRequestAttributeOrigin = HttpRequestAttributePrefix + "request.origin";

		/// <summary>Boolean value, suggesting if the request is a CORS request or not.</summary>
		public const string HttpRequestAttributeIsCorsRequest = HttpRequestAttributePrefix + "isCorsRequest";

		/// <summary>
		/// Type of CORS request, of type
		/// <see cref="CORSRequestType"/>
		/// .
		/// </summary>
		public const string HttpRequestAttributeRequestType = HttpRequestAttributePrefix + "request.type";

		/// <summary>
		/// Request headers sent as 'Access-Control-Request-Headers' header, for
		/// pre-flight request.
		/// </summary>
		public const string HttpRequestAttributeRequestHeaders = HttpRequestAttributePrefix + "request.headers";

		/// <summary>Enumerates varies types of CORS requests.</summary>
		/// <remarks>
		/// Enumerates varies types of CORS requests. Also, provides utility methods
		/// to determine the request type.
		/// </remarks>
		public enum CORSRequestType
		{
			Simple,
			Actual,
			PreFlight,
			NotCors,
			InvalidCors
		}

		/// <summary>
		/// <see cref="System.Collections.ICollection{E}"/>
		/// of HTTP methods. Case sensitive.
		/// </summary>
		/// <seealso>http://tools.ietf.org/html/rfc2616#section-5.1.1</seealso>
		public static readonly ICollection<string> HttpMethods = new HashSet<string>(Arrays.AsList("OPTIONS", "GET", "HEAD", "POST", "PUT", "DELETE", "TRACE", "CONNECT"));

		/// <summary>
		/// <see cref="System.Collections.ICollection{E}"/>
		/// of non-simple HTTP methods. Case sensitive.
		/// </summary>
		public static readonly ICollection<string> ComplexHttpMethods = new HashSet<string>(Arrays.AsList("PUT", "DELETE", "TRACE", "CONNECT"));

		/// <summary>
		/// <see cref="System.Collections.ICollection{E}"/>
		/// of Simple HTTP methods. Case sensitive.
		/// </summary>
		/// <seealso>http://www.w3.org/TR/cors/#terminology</seealso>
		public static readonly ICollection<string> SimpleHttpMethods = new HashSet<string>(Arrays.AsList("GET", "POST", "HEAD"));

		/// <summary>
		/// <see cref="System.Collections.ICollection{E}"/>
		/// of Simple HTTP request headers. Case in-sensitive.
		/// </summary>
		/// <seealso>http://www.w3.org/TR/cors/#terminology</seealso>
		public static readonly ICollection<string> SimpleHttpRequestHeaders = new HashSet<string>(Arrays.AsList("Accept", "Accept-Language", "Content-Language"));

		/// <summary>
		/// <see cref="System.Collections.ICollection{E}"/>
		/// of Simple HTTP request headers. Case in-sensitive.
		/// </summary>
		/// <seealso>http://www.w3.org/TR/cors/#terminology</seealso>
		public static readonly ICollection<string> SimpleHttpResponseHeaders = new HashSet<string>(Arrays.AsList("Cache-Control", "Content-Language", "Content-Type", "Expires", "Last-Modified", "Pragma"));

		/// <summary>
		/// <see cref="System.Collections.ICollection{E}"/>
		/// of Simple HTTP request headers. Case in-sensitive.
		/// </summary>
		/// <seealso>http://www.w3.org/TR/cors/#terminology</seealso>
		public static readonly ICollection<string> SimpleHttpRequestContentTypeValues = new HashSet<string>(Arrays.AsList("application/x-www-form-urlencoded", "multipart/form-data", "text/plain"));

		/// <summary>By default, all origins are allowed to make requests.</summary>
		public const string DefaultAllowedOrigins = "*";

		/// <summary>By default, following methods are supported: GET, POST, HEAD and OPTIONS.</summary>
		public const string DefaultAllowedHttpMethods = "GET,POST,HEAD,OPTIONS";

		/// <summary>By default, time duration to cache pre-flight response is 30 mins.</summary>
		public const string DefaultPreflightMaxage = "1800";

		/// <summary>By default, support credentials is turned on.</summary>
		public const string DefaultSupportsCredentials = "true";

		/// <summary>
		/// By default, following headers are supported:
		/// Origin,Accept,X-Requested-With, Content-Type,
		/// Access-Control-Request-Method, and Access-Control-Request-Headers.
		/// </summary>
		public const string DefaultAllowedHttpHeaders = "Origin,Accept,X-Requested-With,Content-Type," + "Access-Control-Request-Method,Access-Control-Request-Headers";

		/// <summary>By default, none of the headers are exposed in response.</summary>
		public const string DefaultExposedHeaders = string.Empty;

		/// <summary>By default, access log logging is turned off</summary>
		public const string DefaultLoggingEnabled = "false";

		/// <summary>By default, request is decorated with CORS attributes.</summary>
		public const string DefaultDecorateRequest = "true";

		/// <summary>
		/// Key to retrieve allowed origins from
		/// <see cref="Javax.Servlet.IFilterConfig"/>
		/// .
		/// </summary>
		public const string ParamCorsAllowedOrigins = "cors.allowed.origins";

		/// <summary>
		/// Key to retrieve support credentials from
		/// <see cref="Javax.Servlet.IFilterConfig"/>
		/// .
		/// </summary>
		public const string ParamCorsSupportCredentials = "cors.support.credentials";

		/// <summary>
		/// Key to retrieve exposed headers from
		/// <see cref="Javax.Servlet.IFilterConfig"/>
		/// .
		/// </summary>
		public const string ParamCorsExposedHeaders = "cors.exposed.headers";

		/// <summary>
		/// Key to retrieve allowed headers from
		/// <see cref="Javax.Servlet.IFilterConfig"/>
		/// .
		/// </summary>
		public const string ParamCorsAllowedHeaders = "cors.allowed.headers";

		/// <summary>
		/// Key to retrieve allowed methods from
		/// <see cref="Javax.Servlet.IFilterConfig"/>
		/// .
		/// </summary>
		public const string ParamCorsAllowedMethods = "cors.allowed.methods";

		/// <summary>
		/// Key to retrieve preflight max age from
		/// <see cref="Javax.Servlet.IFilterConfig"/>
		/// .
		/// </summary>
		public const string ParamCorsPreflightMaxage = "cors.preflight.maxage";

		/// <summary>Key to retrieve access log logging flag.</summary>
		public const string ParamCorsLoggingEnabled = "cors.logging.enabled";

		/// <summary>Key to determine if request should be decorated.</summary>
		public const string ParamCorsRequestDecorate = "cors.request.decorate";
		// -------------------------------------------------- CORS Response Headers
		// -------------------------------------------------- CORS Request Headers
		// ----------------------------------------------------- Request attributes
		// -------------------------------------------------------------- Constants
		// ------------------------------------------------ Configuration Defaults
		// ----------------------------------------Filter Config Init param-name(s)
	}
}
