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

#include "UuidBox.h"
#include "BoxCollection.h"

CUuidBox::CUuidBox(HRESULT *result)
  : CBox(result)
{
  this->type = NULL;
  this->guid.Data1 = 0;
  this->guid.Data2 = 0;
  this->guid.Data3 = 0;
  this->guid.Data4[0] = 0;
  this->guid.Data4[1] = 0;
  this->guid.Data4[2] = 0;
  this->guid.Data4[3] = 0;
  this->guid.Data4[4] = 0;
  this->guid.Data4[5] = 0;
  this->guid.Data4[6] = 0;
  this->guid.Data4[7] = 0;
  this->payload = NULL;
  this->payloadSize = 0;

  if ((result != NULL) && (SUCCEEDED(*result)))
  {
    this->type = Duplicate(UUID_BOX_TYPE);

    CHECK_POINTER_HRESULT(*result, this->type, *result, E_OUTOFMEMORY);
  }
}

CUuidBox::~CUuidBox(void)
{
  FREE_MEM(this->payload);
}

/* get methods */

GUID CUuidBox::GetGuid(void)
{
  return this->guid;
}


const uint8_t *CUuidBox::GetPayload(void)
{
  return this->payload;
}

uint64_t CUuidBox::GetPayloadSize(void)
{
  return this->payloadSize;
}

/* set methods */

void CUuidBox::SetGuid(GUID guid)
{
  this->guid = guid;
}

bool CUuidBox::SetPayload(const uint8_t *buffer, uint32_t length)
{
  bool result = (buffer != NULL) || (length == 0);
  FREE_MEM(this->payload);
  this->payloadSize = 0;

  if (result)
  {
    if (length > 0)
    {
      this->payload = ALLOC_MEM_SET(this->payload, uint8_t, length, 0);
      result &= (this->payload != NULL);

      if (result)
      {
        memcpy(this->payload, buffer, length);
        this->payloadSize = length;
      }
    }
  }

  return result;
}

/* other methods */

wchar_t *CUuidBox::GetParsedHumanReadable(const wchar_t *indent)
{
  wchar_t *result = NULL;
  wchar_t *previousResult = __super::GetParsedHumanReadable(indent);

  if ((previousResult != NULL) && (this->IsParsed()))
  {
    wchar_t *guid = ConvertGuidToString(this->GetGuid());
    // prepare finally human readable representation
    result = FormatString(
      L"%s\n" \
      L"%sGUID: %s\n",
      L"%sPayload size: %llu"
      ,
      
      previousResult,
      indent, guid,
      indent, this->GetPayloadSize()
      );

    FREE_MEM(guid);
  }

  FREE_MEM(previousResult);

  return result;
}

uint64_t CUuidBox::GetBoxSize(void)
{
  uint64_t result = 16 + this->GetPayloadSize();

  if (result != 0)
  {
    uint64_t boxSize = __super::GetBoxSize();
    result = (boxSize != 0) ? (result + boxSize) : 0; 
  }

  return result;
}

bool CUuidBox::ParseInternal(const unsigned char *buffer, uint32_t length, bool processAdditionalBoxes)
{
  FREE_MEM(this->payload);
  this->payloadSize = 0;

  if (__super::ParseInternal(buffer, length, false))
  {
    this->flags &= ~BOX_FLAG_PARSED;
    this->flags |= (wcscmp(this->type, UUID_BOX_TYPE) == 0) ? BOX_FLAG_PARSED : BOX_FLAG_NONE;

    if (this->IsSetFlags(BOX_FLAG_PARSED))
    {
      // box is UUID box, parse all values
      uint32_t position = this->HasExtendedHeader() ? BOX_HEADER_LENGTH_SIZE64 : BOX_HEADER_LENGTH;
      HRESULT continueParsing = (this->GetSize() <= (uint64_t)length) ? S_OK : E_NOT_VALID_STATE;

      if (SUCCEEDED(continueParsing))
      {
        RBE32INC(buffer, position, this->guid.Data1);
        RBE16INC(buffer, position, this->guid.Data2);
        RBE16INC(buffer, position, this->guid.Data3);
        RBE8INC(buffer, position, this->guid.Data4[0]);
        RBE8INC(buffer, position, this->guid.Data4[1]);
        RBE8INC(buffer, position, this->guid.Data4[2]);
        RBE8INC(buffer, position, this->guid.Data4[3]);
        RBE8INC(buffer, position, this->guid.Data4[4]);
        RBE8INC(buffer, position, this->guid.Data4[5]);
        RBE8INC(buffer, position, this->guid.Data4[6]);
        RBE8INC(buffer, position, this->guid.Data4[7]);
      }

      // UUID box can be larger than minimum required size, but content is not clear
      // we don't know if in box are other boxes or it is custom data

      this->payloadSize = this->GetSize() - position;
      CHECK_CONDITION_HRESULT(continueParsing, (position + this->payloadSize) <= length, continueParsing, E_NOT_VALID_STATE);

      if (SUCCEEDED(continueParsing))
      {
        this->payload = ALLOC_MEM_SET(this->payload, uint8_t, (uint32_t)this->payloadSize, 0);
        CHECK_POINTER_HRESULT(continueParsing, this->payload, continueParsing, E_OUTOFMEMORY);

        CHECK_CONDITION_EXECUTE(SUCCEEDED(continueParsing), memcpy(this->payload, buffer + position, (uint32_t)this->payloadSize));
        position += (uint32_t)this->payloadSize;
      }

      /*if (SUCCEEDED(continueParsing) && processAdditionalBoxes)
      {
        this->ProcessAdditionalBoxes(buffer, length, position);
      }*/

      this->flags &= ~BOX_FLAG_PARSED;
      this->flags |= SUCCEEDED(continueParsing) ? BOX_FLAG_PARSED : BOX_FLAG_NONE;
    }
  }

  return this->IsSetFlags(BOX_FLAG_PARSED);
}

uint32_t CUuidBox::GetBoxInternal(uint8_t *buffer, uint32_t length, bool processAdditionalBoxes)
{
  uint32_t result = __super::GetBoxInternal(buffer, length, false);

  if (result != 0)
  {
    WBE32INC(buffer, result, this->GetGuid().Data1);
    WBE16INC(buffer, result, this->GetGuid().Data2);
    WBE16INC(buffer, result, this->GetGuid().Data3);
    WBE8INC(buffer, result, this->GetGuid().Data4[0]);
    WBE8INC(buffer, result, this->GetGuid().Data4[1]);
    WBE8INC(buffer, result, this->GetGuid().Data4[2]);
    WBE8INC(buffer, result, this->GetGuid().Data4[3]);
    WBE8INC(buffer, result, this->GetGuid().Data4[4]);
    WBE8INC(buffer, result, this->GetGuid().Data4[5]);
    WBE8INC(buffer, result, this->GetGuid().Data4[6]);
    WBE8INC(buffer, result, this->GetGuid().Data4[7]);

    if (this->GetPayloadSize() > 0)
    {
      memcpy(buffer + result, this->GetPayload(), (uint32_t)this->GetPayloadSize());
      result += (uint32_t)this->GetPayloadSize();
    }

    if ((result != 0) && processAdditionalBoxes && (this->GetBoxes()->Count() != 0))
    {
      uint32_t boxSizes = this->GetAdditionalBoxes(buffer + result, length - result);
      result = (boxSizes != 0) ? (result + boxSizes) : 0;
    }
  }

  return result;
}