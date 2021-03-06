﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using UploadcareCSharp.Data;
using UploadcareCSharp.Exceptions;
using UploadcareCSharp.Url.UrlParameters;

namespace UploadcareCSharp.API
{
	/// <summary>
	/// A helper class for doing API calls to the Uploadcare API. Supports API version 0.3.
	/// 
	/// TODO Support of throttled requests needs to be added
	/// </summary>
	internal class RequestHelper
	{
		private readonly Client _client;

		internal RequestHelper(Client client)
		{
			_client = client;
		}

        public IEnumerable<T> ExecutePaginatedQuery<T, TU, TK>(Uri url, List<IUrlParameter> urlParameters,
            bool includeApiHeaders, TK pageData, IDataWrapper<T, TU> dataWrapper)
	    {
            return new FilesEnumator<T, TU, TK>(this, url, urlParameters, includeApiHeaders, pageData, dataWrapper); 
	    }

        public T ExecuteQuery<T>(HttpWebRequest request, bool includeApiHeaders, T dataClass)
	    {
            try
            {
                if (includeApiHeaders)
                    AddApiHeaders(request);

                var response = (HttpWebResponse) request.GetResponse();

                CheckResponseStatus(response);

                using (var dataStream = response.GetResponseStream())
                {
                    using (var reader = new StreamReader(dataStream))
                    {
                        var responseFromServer = reader.ReadToEnd();
                        return JsonConvert.DeserializeObject<T>(responseFromServer);
                    }
                }
            }
            catch (WebException e)
            {
                //TODO: parse response and show only error message
                throw new UploadcareInvalidRequestException(StreamToString(e.Response.GetResponseStream()));
            }
	        catch (IOException e)
	        {
	            throw new UploadcareNetworkException(e);
	        }
	    }

	    /// <summary>
	    /// Executes the request et the Uploadcare API and return the HTTP Response object.
	    /// 
	    /// The existence of this method(and it's return type) enables the end user to extend the functionality of the
	    /// Uploadcare API client by creating a subclass of <seealso cref="Client"/>.
	    /// </summary>
	    /// <param name="request"> request to be sent to the API </param>
        /// <param name="includeApiHeaders">TRUE if the default API headers should be set</param>
	    /// <returns> HTTP Response object </returns>
	    public HttpWebResponse ExecuteCommand(HttpWebRequest request, bool includeApiHeaders)
		{
			try
            {
                if (includeApiHeaders)
                    AddApiHeaders(request);

                var response = (HttpWebResponse)request.GetResponse();
				CheckResponseStatus(response);

				return response;
			}
			catch (IOException e)
			{
				throw new UploadcareNetworkException(e);
			}
		}

	    /// <summary>
        /// Verifies that the response status codes are within acceptable boundaries and throws corresponding exceptions
        /// otherwise.
        /// </summary>
        /// <param name="response"> The response object to be checked </param>
        /// <exception cref="IOException"> </exception>
        private static void CheckResponseStatus(HttpWebResponse response)
        {
            var statusCode = response.StatusCode;

            if (statusCode >= HttpStatusCode.OK && statusCode < HttpStatusCode.Ambiguous)
            {
                return;
            }
            switch (statusCode)
            {
                case HttpStatusCode.Unauthorized:
                case HttpStatusCode.Forbidden:
                    throw new UploadcareAuthenticationException(StreamToString(response.GetResponseStream()));
                case HttpStatusCode.BadRequest:
                case HttpStatusCode.NotFound:
                    throw new UploadcareInvalidRequestException(StreamToString(response.GetResponseStream()));
            }

            throw new UploadcareApiException("Unknown exception during an API call, response:" + StreamToString(response.GetResponseStream()));
        }

        private void AddApiHeaders(HttpWebRequest request)
        {
            request.Accept = "application/vnd.uploadcare-v0.3+json";
            request.Date = DateTime.Now;

            if (_client.SimpleAuth)
                request.Headers.Set("Authorization", string.Format("Uploadcare.Simple {0}:{1}", _client.PublicKey, _client.PrivateKey));
            else
            {
                //TODO: any another auth?
            }
        }

        /// <summary>
        /// Convert an InputStream into a String object</summary>
        /// <param name="stream"> The stream to be converted </param>
        /// <returns> The resulting String </returns>
        private static string StreamToString(Stream stream)
        {
            var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}