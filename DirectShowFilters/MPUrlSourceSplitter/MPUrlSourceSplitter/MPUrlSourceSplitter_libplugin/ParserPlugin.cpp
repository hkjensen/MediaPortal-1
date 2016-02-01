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

#include "StdAfx.h"

#pragma warning(push)
// disable warning: 'INT8_MIN' : macro redefinition
// warning is caused by stdint.h and intsafe.h, which both define same macro
#pragma warning(disable:4005)

#include "ParserPlugin.h"
#include "ParserPluginConfiguration.h"
#include "Parameters.h"

#include <Shlwapi.h>

#pragma warning(pop)

CParserPlugin::CParserPlugin(HRESULT *result, CLogger *logger, CParameterCollection *configuration)
  : CPlugin(result, logger, configuration)
{
  this->protocolHoster = NULL;
  this->parserResult = PARSER_RESULT_PENDING;
  this->connectionParameters = NULL;
  this->reportedStreamTime = 0;
  this->reportedStreamPosition = 0;
  this->dumpFile = NULL;

  if ((result != NULL) && (SUCCEEDED(*result)))
  {
    this->connectionParameters = new CParameterCollection(result);
    this->dumpFile = new CDumpFile(result);

    CHECK_POINTER_HRESULT(*result, this->connectionParameters, *result, E_OUTOFMEMORY);
    CHECK_POINTER_HRESULT(*result, this->dumpFile, *result, E_OUTOFMEMORY);

    CHECK_CONDITION_HRESULT(*result, this->configuration->Append(configuration), *result, E_OUTOFMEMORY);
  }
}

CParserPlugin::~CParserPlugin(void)
{
  FREE_MEM_CLASS(this->connectionParameters);
  FREE_MEM_CLASS(this->dumpFile);
}

// CPlugin

HRESULT CParserPlugin::Initialize(CPluginConfiguration *configuration)
{
  HRESULT result = __super::Initialize(configuration);

  if (SUCCEEDED(result))
  {
    CParserPluginConfiguration *parserConfiguration = dynamic_cast<CParserPluginConfiguration *>(configuration);
    CHECK_POINTER_HRESULT(result, parserConfiguration, result, E_INVALIDARG);

    if (SUCCEEDED(result))
    {
      this->protocolHoster = parserConfiguration->GetProtocolHoster();
    }
  }

  return result;
}

// IDemuxerOwner interface

int64_t CParserPlugin::GetDuration(void)
{
  return this->protocolHoster->GetDuration();
}

HRESULT CParserPlugin::ProcessStreamPackage(CStreamPackage *streamPackage)
{
  return this->protocolHoster->ProcessStreamPackage(streamPackage);
}

HRESULT CParserPlugin::QueryStreamProgress(CStreamProgress *streamProgress)
{
  return this->protocolHoster->QueryStreamProgress(streamProgress);
}

// IProtocol interface

HRESULT CParserPlugin::GetConnectionParameters(CParameterCollection *parameters)
{
  HRESULT result = S_OK;
  CHECK_POINTER_DEFAULT_HRESULT(result, parameters);

  CHECK_CONDITION_HRESULT(result, parameters->Append(this->connectionParameters), result, E_OUTOFMEMORY);

  return result;
}

ProtocolConnectionState CParserPlugin::GetConnectionState(void)
{
  return None;
}

HRESULT CParserPlugin::ParseUrl(const CParameterCollection *parameters)
{
  return S_OK;
}

HRESULT CParserPlugin::ReceiveData(CStreamPackage *streamPackage)
{
  return S_OK;
}

// ISimpleProtocol interface

unsigned int CParserPlugin::GetOpenConnectionTimeout(void)
{
  return this->protocolHoster->GetOpenConnectionTimeout();
}

unsigned int CParserPlugin::GetOpenConnectionSleepTime(void)
{
  return this->protocolHoster->GetOpenConnectionSleepTime();
}

unsigned int CParserPlugin::GetTotalReopenConnectionTimeout(void)
{
  return this->protocolHoster->GetTotalReopenConnectionTimeout();
}

HRESULT CParserPlugin::StartReceivingData(CParameterCollection *parameters)
{
  return S_OK;
}

HRESULT CParserPlugin::StopReceivingData(void)
{
  return S_OK;
}

void CParserPlugin::ClearSession(void)
{
  CPlugin::ClearSession();

  this->parserResult = PARSER_RESULT_PENDING;
  this->connectionParameters->Clear();
  this->reportedStreamTime = 0;
  this->reportedStreamPosition = 0;
  this->dumpFile->FlushDumpBoxes();
  this->dumpFile->SetDumpFile(NULL);
}

void CParserPlugin::ReportStreamTime(uint64_t streamTime, uint64_t streamPosition)
{
  this->reportedStreamTime = streamTime;
  this->reportedStreamPosition = streamPosition;

  this->protocolHoster->ReportStreamTime(streamTime, streamPosition);
}

HRESULT CParserPlugin::GetStreamInformation(CStreamInformationCollection *streams)
{
  return this->protocolHoster->GetStreamInformation(streams);
}

// ISeeking interface

unsigned int CParserPlugin::GetSeekingCapabilities(void)
{
  return this->protocolHoster->GetSeekingCapabilities();
}

/* get methods */

HRESULT CParserPlugin::GetParserResult(void)
{
  return this->parserResult;
}

int64_t CParserPlugin::SeekToTime(unsigned int streamId, int64_t time)
{
  return this->protocolHoster->SeekToTime(streamId, time);
}

void CParserPlugin::SetPauseSeekStopMode(unsigned int pauseSeekStopMode)
{
  this->protocolHoster->SetPauseSeekStopMode(pauseSeekStopMode);
}

unsigned int CParserPlugin::GetIptvSectionCount(void)
{
  return 0;
}

HRESULT CParserPlugin::GetIptvSection(unsigned int index, wchar_t **section)
{
  return E_NOTIMPL;
}

/* set methods */

HRESULT CParserPlugin::SetConnectionParameters(const CParameterCollection *parameters)
{
  HRESULT result = S_OK;
  CHECK_POINTER_DEFAULT_HRESULT(result, parameters);

  CHECK_CONDITION_HRESULT(result, this->connectionParameters->Append((CParameterCollection *)parameters), result, E_OUTOFMEMORY);

  if (SUCCEEDED(result))
  {
    this->flags &= ~(PARSER_PLUGIN_FLAG_DUMP_INPUT_DATA | PARSER_PLUGIN_FLAG_DUMP_OUTPUT_DATA | PLUGIN_FLAG_SPLITTER | PLUGIN_FLAG_IPTV);

    this->flags |= this->connectionParameters->GetValueBool(PARAMETER_NAME_SPLITTER, true, PARAMETER_NAME_SPLITTER_DEFAULT) ? PLUGIN_FLAG_SPLITTER : PROTOCOL_PLUGIN_FLAG_NONE;
    this->flags |= this->connectionParameters->GetValueBool(PARAMETER_NAME_IPTV, true, PARAMETER_NAME_IPTV_DEFAULT) ? PLUGIN_FLAG_IPTV : PROTOCOL_PLUGIN_FLAG_NONE;
    this->flags |= this->connectionParameters->GetValueBool(PARAMETER_NAME_DUMP_PARSER_INPUT_DATA, true, PARAMETER_NAME_DUMP_PARSER_INPUT_DATA_DEFAULT) ? PARSER_PLUGIN_FLAG_DUMP_INPUT_DATA : PARSER_PLUGIN_FLAG_NONE;
    this->flags |= this->connectionParameters->GetValueBool(PARAMETER_NAME_DUMP_PARSER_OUTPUT_DATA, true, PARAMETER_NAME_DUMP_PARSER_OUTPUT_DATA_DEFAULT) ? PARSER_PLUGIN_FLAG_DUMP_OUTPUT_DATA : PARSER_PLUGIN_FLAG_NONE;

    if (this->IsDumpInputData() || this->IsDumpOutputData())
    {
      wchar_t *storeFilePath = this->GetDumpFile();
      CHECK_CONDITION_NOT_NULL_EXECUTE(storeFilePath, this->dumpFile->SetDumpFile(storeFilePath));
      FREE_MEM(storeFilePath);
    }
  }

  return result;
}

/* other methods */

bool CParserPlugin::IsLiveStreamSpecified(void)
{
  return this->protocolHoster->IsLiveStreamSpecified();
}

bool CParserPlugin::IsLiveStreamDetected(void)
{
  return this->protocolHoster->IsLiveStreamDetected();
}

bool CParserPlugin::IsLiveStream(void)
{
  return this->protocolHoster->IsLiveStream();
}

bool CParserPlugin::IsSetStreamLength(void)
{
  return this->protocolHoster->IsSetStreamLength();
}

bool CParserPlugin::IsStreamLengthEstimated(void)
{
  return this->protocolHoster->IsStreamLengthEstimated();
}

bool CParserPlugin::IsWholeStreamDownloaded(void)
{
  return this->protocolHoster->IsWholeStreamDownloaded();
}

bool CParserPlugin::IsEndOfStreamReached(void)
{
  return this->protocolHoster->IsEndOfStreamReached();
}

bool CParserPlugin::IsConnectionLostCannotReopen(void)
{
  return this->protocolHoster->IsConnectionLostCannotReopen();
}

bool CParserPlugin::IsStreamIptvCompatible(void)
{
  return false;
}

bool CParserPlugin::IsDumpInputData(void)
{
  return this->IsSetFlags(PARSER_PLUGIN_FLAG_DUMP_INPUT_DATA);
}

bool CParserPlugin::IsDumpOutputData(void)
{
  return this->IsSetFlags(PARSER_PLUGIN_FLAG_DUMP_OUTPUT_DATA);
}

/* protected methods */

wchar_t *CParserPlugin::GetCacheFile(const wchar_t *extra)
{
  wchar_t *result = NULL;
  const wchar_t *folder = this->connectionParameters->GetValue(PARAMETER_NAME_CACHE_FOLDER, true, NULL);

  if (folder != NULL)
  {
    wchar_t *guid = ConvertGuidToString(this->logger->GetLoggerInstanceId());

    if (guid != NULL)
    {
      if (IsNullOrEmpty(extra))
      {
        result = FormatString(L"%s%s_%s.temp", folder, this->GetStoreFileNamePart(), guid);
      }
      else
      {
        result = FormatString(L"%s%s_%s_%s.temp", folder, this->GetStoreFileNamePart(), guid, extra);
      }
    }

    FREE_MEM(guid);
  }

  return result;
}

wchar_t *CParserPlugin::GetDumpFile(const wchar_t *extra)
{
  wchar_t *result = NULL;
  wchar_t *folder = Duplicate(this->connectionParameters->GetValue(PARAMETER_NAME_LOG_FILE_NAME, true, NULL));

  if (folder != NULL)
  {
    PathRemoveFileSpec(folder);

    wchar_t *guid = ConvertGuidToString(this->logger->GetLoggerInstanceId());
    if (guid != NULL)
    {
      if (IsNullOrEmpty(extra))
      {
        result = FormatString(L"%s\\%s_%s.dump", folder, this->GetStoreFileNamePart(), guid);
      }
      else
      {
        result = FormatString(L"%s\\%s_%s_%s.dump", folder, this->GetStoreFileNamePart(), guid, extra);
      }
    }
    FREE_MEM(guid);
  }

  FREE_MEM(folder);

  return result;
}

wchar_t *CParserPlugin::GetDumpFile(void)
{
  return this->GetDumpFile(NULL);
}