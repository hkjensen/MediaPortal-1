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

#ifndef __CURL_INSTANCE_DEFINED
#define __CURL_INSTANCE_DEFINED

#include "Logger.h"
#include "LinearBuffer.h"
#include "DownloadRequest.h"
#include "DownloadResponse.h"
#include "Flags.h"
#include "DumpFile.h"
#include "IProtocol.h"

#include <curl/curl.h>

FORCEINLINE HRESULT HRESULT_FROM_CURL_CODE(CURLcode curlCode) { return ((curlCode != 0) ? (HRESULT)(curlCode | 0xFFFFF000) : 0); }
FORCEINLINE HRESULT HRESULT_FROM_CURLM_CODE(CURLMcode curlmCode) { return ((curlmCode != 0) ? (HRESULT)(curlmCode | 0xFFFFF080) : 0); }

FORCEINLINE bool IS_CURL_ERROR(HRESULT error) { return ((error & 0xFFFFFF00) == 0xFFFFF000); }

#define CURL_INSTANCE_FLAG_NONE                                               FLAGS_NONE

#define CURL_INSTANCE_FLAG_DUMP_INPUT_DATA                                    (1 << (FLAGS_LAST + 0))
#define CURL_INSTANCE_FLAG_DUMP_OUTPUT_DATA                                   (1 << (FLAGS_LAST + 1))

#define CURL_INSTANCE_FLAG_LAST                                               (FLAGS_LAST + 2)

#define METHOD_CREATE_CURL_WORKER_NAME                                        L"CreateCurlWorker()"
#define METHOD_DESTROY_CURL_WORKER_NAME                                       L"DestroyCurlWorker()"
#define METHOD_CURL_WORKER_NAME                                               L"CurlWorker()"
#define METHOD_CURL_DEBUG_CALLBACK                                            L"CurlDebugCallback()"
#define METHOD_INITIALIZE_NAME                                                L"Initialize()"
#define METHOD_CURL_DEBUG_NAME                                                L"CurlDebug()"
#define METHOD_CURL_RECEIVE_DATA_NAME                                         L"CurlReceiveData()"

#define METHOD_CURL_ERROR_MESSAGE                                             L"%s: %s: %s: %s"

#define CURL_STATE_NONE                                                       0
#define CURL_STATE_CREATED                                                    1
#define CURL_STATE_INITIALIZED                                                2
#define CURL_STATE_RECEIVING_DATA                                             3
#define CURL_STATE_RECEIVED_ALL_DATA                                          4

#define MINIMUM_BUFFER_SIZE                                                   256 * 1024

class CCurlInstance : public CFlags
{
public:
  // initializes a new instance of CCurlInstance class
  // @param logger : logger for logging purposes
  // @param mutex : mutex for locking access to receive data buffer
  // @param protocolName : the protocol name instantiating
  // @param instanceName : the name of CURL instance
  CCurlInstance(HRESULT *result, CLogger *logger, HANDLE mutex, const wchar_t *protocolName, const wchar_t *instanceName);

  // destructor
  virtual ~CCurlInstance(void);

  /* get methods */

  // gets CURL state
  // @return : one of CURL_STATE values
  virtual unsigned int GetCurlState(void);

  // gets libcurl version
  // caller is responsible for freeing memory
  // @return : libcurl version or NULL if error
  static wchar_t *GetCurlVersion(void);

  // gets download request
  // @return : download request
  virtual CDownloadRequest *GetDownloadRequest(void);

  // gets download response
  // @return : download respose
  virtual CDownloadResponse *GetDownloadResponse(void);

  // gets dump file name
  // @return : dump file name or NULL if error or not set
  virtual const wchar_t *GetDumpFile(void);

  // gets owner of CURL instance
  // @return : reference to owner of M3U8 CURL instance (if locked), NULL otherwise
  virtual void *GetOwner(void);

  // gets owner lock count of CURL instance
  // @return : owner lock count of CURL instance, zero if not locked
  virtual unsigned int GetOwnerLockCount(void);

  // gets CURL instance connection state
  // @return : connection state
  virtual ProtocolConnectionState GetConnectionState(void);

  /* set methods */

  // sets dump file name
  // @param dumpFile : the dump file name
  // @return : true if successful, false otherwise
  virtual bool SetDumpFile(const wchar_t *dumpFile);

  // sets dump input data flag
  // @return : true to set, false otherwise
  virtual void SetDumpInputData(bool dumpInputData);

  // sets dump output data flag
  // @return : true to set, false otherwise
  virtual void SetDumpOutputData(bool dumpOutputData);

  // sets CURL instance connection state
  // @param connectionState : the connection state to set
  virtual void SetConnectionState(ProtocolConnectionState connectionState);

  /* other methods */

  // initializes CURL instance
  // @param downloadRequest : download request
  // @return : S_OK if successful, error code otherwise
  virtual HRESULT Initialize(CDownloadRequest *downloadRequest);

  // starts receiving data
  // @return : true if successful, false otherwise
  virtual HRESULT StartReceivingData(void);

  // stops receiving data
  // @return : true if successful, false otherwise
  virtual HRESULT StopReceivingData(void);

  // lock CURL instance for specific owner
  // only owner can unlock instance for other use
  // @param owner : the requested owner to lock instance
  // @return : S_OK if locked, S_FALSE if already locked by same owner, E_FAIL if locked by another owner
  virtual HRESULT LockCurlInstance(void *owner);

  // unlocks CURL instance for specific owner
  // only owner can unlock instance for other use
  // @param owner : the requested owner to unlock instance
  // @return : S_OK if unlocked, S_FALSE if still locked by same owner, E_FAIL if locked by another owner or instance not locked
  virtual HRESULT UnlockCurlInstance(void *owner);

  // tests if CURL instance is locked
  // @return : true if instance is locked, false otherwise
  virtual bool IsLockedCurlInstance(void);

  // tests if CURL instance is locked by specified owner
  // @param owner : the owner to test lock
  // @return : true if instance is locked by owner, false otherwise
  virtual bool IsLockedCurlInstanceByOwner(void *owner);

  // sets string to CURL option
  // @param curl : curl handle to set CURL option
  // @param option : CURL option to set
  // @param string : the string to set to CURL option
  // @return : CURLE_OK if successful, error code otherwise
  static HRESULT SetString(CURL *curl, CURLoption option, const wchar_t *string);

  // sends data through current CURL instance
  // @param data : data to send
  // @param length : the length of data to send
  // @param timeout : the timeout in us (microseconds) for sending data
  // @return : CURLE_OK is successful, error code otherwise
  virtual HRESULT SendData(const unsigned char *data, unsigned int length, unsigned int timeout);

  // reads data through current CURL instance
  // @param data : data buffer to store read data
  // @param length : the length of data buffer
  // @return : the length of read data or error code (lower than zero) if error
  virtual HRESULT ReadData(unsigned char *data, unsigned int length);

  // tests if current instance is in readable (there are data to read) or writable (we can send data) state
  // @param read : true if testing for readable state, false otherwise
  // @param write : true if testing for writable state, false otherwise
  // @param timeout : the timeout in us (microseconds), UINT_MAX for no timeout
  // @return : S_OK if successful, E_NOT_VALID_STATE if no CURL instance or can't get internal socket, E_INVALIDARG if read and write are false, error code if another error
  virtual HRESULT Select(bool read, bool write, unsigned int timeout);

  // tests if dump input data flag is set
  // @return : true if set, false otherwise
  virtual bool IsDumpInputData(void);

  // tests if dump output data flag is set
  // @return : true if set, false otherwise
  virtual bool IsDumpOutputData(void);

  // clears session
  virtual void ClearSession(void);

protected:
  CURL *curl;
  CURLM *multi_curl;
  CLogger *logger;
  HANDLE mutex;

  // libcurl worker thread
  HANDLE hCurlWorkerThread;
  
  // holds download request
  CDownloadRequest *downloadRequest;

  // holds download response
  CDownloadResponse *downloadResponse;

  // the protocol implementation name (for logging purposes)
  wchar_t *protocolName;

  // specifies if CURL worker should exit
  volatile bool curlWorkerShouldExit;

  // write callback for CURL
  curl_write_callback writeCallback;

  // user specified data supplied to write callback
  void *writeData;

  // holds internal state
  unsigned int state;

  // holds time when request was sent
  DWORD startReceivingTicks;
  // holds time when receiving was stopped
  DWORD stopReceivingTicks;
  // holds count of bytes received
  int64_t totalReceivedBytes;

  // holds last receive time of any data
  volatile DWORD lastReceiveTime;

  // holds dump file
  CDumpFile *dumpFile;

  // holds owner of CURL instance (if locked)
  void *owner;
  // holds owner lock count (zero if not locked, more than zero if locked)
  unsigned int ownerLockCount;

  // holds connection state
  ProtocolConnectionState connectionState;

  /* methods */

  // virtual CurlWorker() method is called from static CurlWorker() method
  virtual unsigned int CurlWorker(void);

  static unsigned int WINAPI CurlWorker(LPVOID lpParam);

  // creates libcurl worker
  // @return : S_OK if successful
  virtual HRESULT CreateCurlWorker(void);

  // destroys libcurl worker
  // @return : S_OK if successful
  virtual HRESULT DestroyCurlWorker(void);

  // callback function for receiving data from libcurl
  // its default write callback when not specified other callback
  static size_t CurlReceiveDataCallback(char *buffer, size_t size, size_t nmemb, void *userdata);

  // debug callback of libcurl
  // @param handle : the handle / transfer this concerns
  // @param type : what kind of data
  // @param data : points to the data
  // @param size : size of the data pointed to
  // @param userptr : user defined pointer
  static int CurlDebugCallback(CURL *handle, curl_infotype type, char *data, size_t size, void *userptr);

  // called when CURL debug message arives
  // @param type : CURL message type
  // @param data : received CURL message data
  // @param size : size of the data
  virtual void CurlDebug(curl_infotype type, const unsigned char *data, size_t size);

  // process received data
  // @param buffer : buffer with received data
  // @param length : the length of buffer
  // @return : the length of processed data (lower value than length means error)
  virtual size_t CurlReceiveData(const unsigned char *buffer, size_t length);

  // gets new instance of download response
  // @return : new download response or NULL if error
  virtual CDownloadResponse *CreateDownloadResponse(void);

  // sets string to CURL option
  // @param option : CURL option to set
  // @param string : the string to set to CURL option
  // @return : CURLE_OK if successful, error code otherwise
  virtual HRESULT SetString(CURLoption option, const wchar_t *string);

  // sets write callback for CURL
  // @param writeCallback : callback method for writing data received by CURL
  // @param writeData : user specified data supplied to write callback method
  void SetWriteCallback(curl_write_callback writeCallback, void *writeData);

  // creates dump box for dump file
  // @return : dump box or NULL if error
  virtual CDumpBox *CreateDumpBox(void) = 0;

  // converts CURL text data into readable string
  // @param data : points to CURL text data
  // @param size : the size of data
  // @return : readable string or NULL if error
  static wchar_t *ConvertTextData(const unsigned char *data, size_t size);
};

#endif