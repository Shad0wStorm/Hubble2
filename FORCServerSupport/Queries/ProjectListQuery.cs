using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;

using ClientSupport;

namespace FORCServerSupport.Queries
{
    class ProjectListQuery : JSONWebQuery
    {
#if DEVELOPMENT
        FORCServerState m_state = null;
#endif

        public ProjectListQuery()
            : base("user/purchases")
        {

        }

        public SKUDetails[] Run(FORCServerState state, UserDetails user)
        {
#if DEVELOPMENT
            m_state = state;
#endif
            String authHeader = "bearer " + user.SessionToken;
            //AddParameter(FORCServerState.c_machineIdQuery, state.m_manager.MachineIdentifier.GetMachineIdentifier());
            if (!String.IsNullOrEmpty(state.Language))
            {
                AddParameter(FORCServerState.c_language, state.Language);
            }
            if (state.m_manager.IsSteam)
            {
                //AddParameter(FORCServerState.c_steam, "true");
            }
            if (state.m_manager.OculusEnabled)
            {
                //AddParameter(FORCServerState.c_oculus, "true");
            }
            switch (user.AuthenticationType)
            {
                case ServerInterface.AuthenticationType.Epic:
                    {
                        authHeader = "epic " + user.EpicAccessToken;
                        AddHeader(FORCServerState.c_headerAuthToken, authHeader);
                        break;
                    }
                case ServerInterface.AuthenticationType.Steam:
                    {
                        AddHeader(FORCServerState.c_headerAuthToken, authHeader);
                        break;
                    }
                case ServerInterface.AuthenticationType.FORC:
                default:
                    {
                        AddParameter(FORCServerState.c_authToken, user.SessionToken);
                        AddParameter(FORCServerState.c_machineToken, user.AuthenticationToken);
                        break;
                    }
            }

            state.AddTimeStamp(this);

            Dictionary<String, object> loginResponse;

            state.m_message = null;
            // Force use of version 1.0 until the new interface returns a
            // compatible response or the parsing is extended to handle
            // arrays.
            int api = ((user.AuthenticationType==ServerInterface.AuthenticationType.Steam) || true) ? FORCServerState.APIVersion.V3_0 : FORCServerState.APIVersion.V1_1;
            HttpStatusCode response = Execute(state.GetServerAPI(api), out loginResponse, out state.m_message);

            switch (response)
            {
                case HttpStatusCode.OK:
                    {
                        return BuildProjectList(loginResponse);
                    }
            }
            return null;
        }

        /// <summary>
        /// Extract a list of projects from a server json response.
        /// </summary>
        /// <param name="response"></param>
        private SKUDetails[] BuildProjectList(Dictionary<String, object> response)
        {
            const String c_product_name = "product_name";
            const String c_sku_name = "product_sku";
            const String c_testapi = "testapi";
            const String c_serverArgs = "serverargs";
            const String c_gameArgs = "gameargs";
            const String c_sortKey = "sortkey";
            const String c_directory = "directory";
            const String c_downloadThreads = "downloadthreads";
            const String c_highlight = "colour";
            const String c_pageTemplate = "template";
            const String c_filter = "filter";
            const String c_products = "purchases";
            const String c_imageSet = "imageset";
            const String c_boxUri = "box";
            const String c_heroUri = "hero";
            const String c_logoUri = "logo";
            const String c_ratingsSet = "ratings";
            const String c_esrb = "esrb";
            const String c_pegi= "pegi";
            const String c_gameApi = "game_api";
            const String c_gameCode = "patch_notes_game_code";
            const String c_no_details = "no_details";

            if (response != null)
            {
                List<SKUDetails> details = new List<SKUDetails>();
                List<object> source = response.Values.ToList<object>();
                if (response.ContainsKey(c_products))
                {
                    // 3.0 route returns an array object as a single key/value
                    // pair in place of the dictionary pretending to be an array
                    // of 1.1 route. Use the new key if available, falling back
                    // to the raw dictionary if necessary.
                    if (response.Count==1)
                    {
                        source = (response[c_products] as ArrayList).Cast<object>().ToList();
                    }
                }
                foreach (object o in source)
                {
                    Dictionary<String, object> sku = o as Dictionary<String, object>;
                    if (sku != null)
                    {
                        SKUDetails newDetails = new SKUDetails();
                        if (sku.ContainsKey(c_product_name))
                        {
                            newDetails.m_name = sku[c_product_name] as String;
                        }
                        if (sku.ContainsKey(c_sku_name))
                        {
                            newDetails.m_sku = sku[c_sku_name] as String;
                        }
                        if (sku.ContainsKey(c_serverArgs))
                        {
                            newDetails.m_serverArgs = sku[c_serverArgs] as String;
                        }
                        if (sku.ContainsKey(c_gameArgs))
                        {
                            newDetails.m_gameArgs = sku[c_gameArgs] as String;
                        }
                        if (sku.ContainsKey(c_sortKey))
                        {
                            newDetails.m_sortKey = sku[c_sortKey] as String;
                        }
                        if (sku.ContainsKey(c_testapi))
                        {
                            newDetails.m_testAPI = (bool)sku[c_testapi];
                        }
                        if (sku.ContainsKey(c_directory))
                        {
                            newDetails.m_directory = sku[c_directory] as String;
                        }
                        if (sku.ContainsKey(c_highlight))
                        {
                            newDetails.m_highlight = sku[c_highlight] as String;
                        }
                        if (sku.ContainsKey(c_pageTemplate))
                        {
                            newDetails.m_page = sku[c_pageTemplate] as String;
                        }
                        if (sku.ContainsKey(c_filter))
                        {
                            String filters = sku[c_filter] as String;
                            newDetails.m_filters = filters.Split(',');
                        }
                        if ( sku.ContainsKey( c_imageSet ) )
                        {
                            Dictionary<String, object> imageSku = sku[c_imageSet] as Dictionary<String, object>;
                            if ( imageSku != null )
                            {
                                if ( imageSku.ContainsKey( c_boxUri ) )
                                {
                                    newDetails.m_box = imageSku[c_boxUri] as String;
                                }
                                if ( imageSku.ContainsKey( c_heroUri ) )
                                {
                                    newDetails.m_hero = imageSku[c_heroUri] as String;
                                }
                                if ( imageSku.ContainsKey( c_logoUri ) )
                                {
                                    newDetails.m_logo = imageSku[c_logoUri] as String;
                                }
                            }
                        }
                        if ( sku.ContainsKey( c_ratingsSet ) )
                        {
                            Dictionary<String, object> ratingsSku = sku[c_ratingsSet] as Dictionary<String, object>;
                            if ( ratingsSku != null )
                            {
                                if ( ratingsSku.ContainsKey( c_esrb ) )
                                {
                                    newDetails.m_esrbRating = ratingsSku[c_esrb] as String;
                                }
                                if ( ratingsSku.ContainsKey( c_pegi ) )
                                {
                                    newDetails.m_pegiRating = ratingsSku[c_pegi] as String;
                                }
                            }
                        }
                        if ( sku.ContainsKey( c_gameApi ) )
                        {
                            newDetails.m_gameApi = sku[c_gameApi] as String;
                        }
                        if ( sku.ContainsKey( c_gameCode ) )
                        {
                            newDetails.m_gameCode = (int)sku[c_gameCode];
                        }

                        if (sku.ContainsKey(c_downloadThreads))
                        {
                            newDetails.m_maxDownloadThreads = (int)sku[c_downloadThreads];
                        }

                        if (sku.ContainsKey(c_no_details))
                        {
                            newDetails.m_noDetails = (bool)sku[c_no_details];
                        }

                        if (newDetails.m_name == null)
                        {
                            newDetails.m_name = newDetails.m_sku;
                        }
                        if (newDetails.m_sortKey == null)
                        {
                            newDetails.m_sortKey = newDetails.m_name;
                        }
                        if (newDetails.m_directory == null)
                        {
                            newDetails.m_directory = newDetails.m_sku;
                        }

                        if (newDetails.m_sku != null)
                        {
                            details.Add(newDetails);
                        }
                    }
                }
#if DEVELOPMENT
                if (!String.IsNullOrEmpty(m_state.DevKey))
                {
                    List<SKUDetails> imaginaryDetails = new List<SKUDetails>();
                    foreach (SKUDetails copy in details)
                    {
                        if (copy.m_sortKey == m_state.DevKey)
                        {
                            SKUDetails alt = new SKUDetails(copy);
                            alt.m_sku = alt.m_sku + "_EX";
                            alt.m_name = alt.m_name + " (Alt)";
                            alt.m_sortKey = alt.m_sortKey + "EX";
                            if (!String.IsNullOrEmpty(alt.m_gameArgs))
                            {
                                alt.m_gameArgs += " ";
                            }
                            else
                            {
                                alt.m_gameArgs = "";
                            }
                            alt.m_gameArgs += "-experimentalOption";
                            imaginaryDetails.Add(alt);
                        }
                    }
                    foreach (SKUDetails add in imaginaryDetails)
                    {
                        details.Add(add);
                    }
                }
#endif
                return details.ToArray();
            }
            return null;
        }
    }
}
