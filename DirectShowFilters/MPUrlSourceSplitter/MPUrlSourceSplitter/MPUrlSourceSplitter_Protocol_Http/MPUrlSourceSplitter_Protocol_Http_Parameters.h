/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#pragma once

#ifndef __MP_URL_SOURCE_SPLITTER_PROTOCOL_HTTP_PARAMETERS_DEFINED
#define __MP_URL_SOURCE_SPLITTER_PROTOCOL_HTTP_PARAMETERS_DEFINED

#define PARAMETER_NAME_HTTP_OPEN_CONNECTION_TIMEOUT                   L"HttpOpenConnectionTimeout"
#define PARAMETER_NAME_HTTP_OPEN_CONNECTION_SLEEP_TIME                L"HttpOpenConnectionSleepTime"
#define PARAMETER_NAME_HTTP_TOTAL_REOPEN_CONNECTION_TIMEOUT           L"HttpTotalReopenConnectionTimeout"

#define PARAMETER_NAME_HTTP_REFERER                                   L"HttpReferer"
#define PARAMETER_NAME_HTTP_USER_AGENT                                L"HttpUserAgent"
#define PARAMETER_NAME_HTTP_COOKIE                                    L"HttpCookie"
#define PARAMETER_NAME_HTTP_VERSION                                   L"HttpVersion"
#define PARAMETER_NAME_HTTP_IGNORE_CONTENT_LENGTH                     L"HttpIgnoreContentLength"

#define PARAMETER_NAME_HTTP_COOKIES_COUNT                             L"HttpCookiesCount"
#define HTTP_COOKIE_FORMAT_PARAMETER_NAME                             L"HttpCookie%08u"

#define PARAMETER_NAME_HTTP_HEADERS_COUNT                             L"HttpHeadersCount"
#define HTTP_HEADER_FORMAT_PARAMETER_NAME                             L"HttpHeaderName%08u"
#define HTTP_HEADER_FORMAT_PARAMETER_VALUE                            L"HttpHeaderValue%08u"

// special parameter to force supported seeking by position even if server doesn't respond as expected
// (HTTP 206 response code or Accept-Ranges HTTP response header)
#define PARAMETER_NAME_HTTP_SEEKING_SUPPORTED                         L"HttpSeekingSupported"
// special parameter for enabling (by default) or disabling detection of seeking support
#define PARAMETER_NAME_HTTP_SEEKING_SUPPORT_DETECTION                 L"HttpSeekingSupportDetection"

#define PARAMETER_NAME_HTTP_SERVER_AUTHENTICATE                       L"HttpServerAuthenticate"
#define PARAMETER_NAME_HTTP_SERVER_USER_NAME                          L"HttpServerUserName"
#define PARAMETER_NAME_HTTP_SERVER_PASSWORD                           L"HttpServerPassword"

#define PARAMETER_NAME_HTTP_PROXY_SERVER_AUTHENTICATE                 L"HttpProxyServerAuthenticate"
#define PARAMETER_NAME_HTTP_PROXY_SERVER                              L"HttpProxyServer"
#define PARAMETER_NAME_HTTP_PROXY_SERVER_PORT                         L"HttpProxyServerPort"
#define PARAMETER_NAME_HTTP_PROXY_SERVER_USER_NAME                    L"HttpProxyServerUserName"
#define PARAMETER_NAME_HTTP_PROXY_SERVER_PASSWORD                     L"HttpProxyServerPassword"
#define PARAMETER_NAME_HTTP_PROXY_SERVER_TYPE                         L"HttpProxyServerType"

// we should get data in twenty seconds (splitter)
#define HTTP_OPEN_CONNECTION_TIMEOUT_DEFAULT_SPLITTER                 20000
#define HTTP_OPEN_CONNECTION_SLEEP_TIME_DEFAULT_SPLITTER              0
#define HTTP_TOTAL_REOPEN_CONNECTION_TIMEOUT_DEFAULT_SPLITTER         60000

// we should get data in five seconds (iptv)
#define HTTP_OPEN_CONNECTION_TIMEOUT_DEFAULT_IPTV                     5000
#define HTTP_OPEN_CONNECTION_SLEEP_TIME_DEFAULT_IPTV                  0
#define HTTP_TOTAL_REOPEN_CONNECTION_TIMEOUT_DEFAULT_IPTV             60000

#define HTTP_SEEKING_SUPPORTED_DEFAULT                                false
#define HTTP_SEEKING_SUPPORT_DETECTION_DEFAULT                        true

#define HTTP_SERVER_AUTHENTICATE_DEFAULT                              false
#define HTTP_PROXY_SERVER_AUTHENTICATE_DEFAULT                        false

#define HTTP_PROXY_SERVER_PORT_DEFAULT                                1080
#define HTTP_PROXY_SERVER_TYPE_DEFAULT                                HTTP_PROXY_TYPE_HTTP

#endif
