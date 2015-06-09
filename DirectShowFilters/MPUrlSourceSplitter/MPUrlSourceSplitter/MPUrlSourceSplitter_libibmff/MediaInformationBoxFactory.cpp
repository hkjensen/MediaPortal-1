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

#include "MediaInformationBoxFactory.h"
#include "MediaInformationBox.h"
#include "SampleTableBox.h"
#include "SampleDescriptionBox.h"

CMediaInformationBoxFactory::CMediaInformationBoxFactory(HRESULT *result)
  : CBoxFactory(result)
{
}

CMediaInformationBoxFactory::~CMediaInformationBoxFactory(void)
{
}

CBox *CMediaInformationBoxFactory::CreateBox(const uint8_t *buffer, uint32_t length, uint32_t handlerType)
{
  CBox *result = NULL;
  HRESULT continueParsing = ((buffer != NULL) && (length > 0)) ? S_OK : E_INVALIDARG;

  if (SUCCEEDED(continueParsing))
  {
    CBox *box = new CBox(&continueParsing);
    CHECK_POINTER_HRESULT(continueParsing, box, continueParsing, E_OUTOFMEMORY);

    if (SUCCEEDED(continueParsing))
    {
      CHECK_CONDITION_HRESULT(continueParsing, box->Parse(buffer, length), continueParsing, E_FAIL);

      if (SUCCEEDED(continueParsing))
      {
        CREATE_SPECIFIC_BOX_HANDLER_TYPE(box, MEDIA_INFORMATION_BOX_TYPE, CMediaInformationBox, buffer, length, continueParsing, result, handlerType);
        CREATE_SPECIFIC_BOX_HANDLER_TYPE(box, SAMPLE_TABLE_BOX_TYPE, CSampleTableBox, buffer, length, continueParsing, result, handlerType);
        CREATE_SPECIFIC_BOX_HANDLER_TYPE(box, SAMPLE_DESCRIPTION_BOX_TYPE, CSampleDescriptionBox, buffer, length, continueParsing, result, handlerType);

        if (SUCCEEDED(continueParsing) && (result == NULL))
        {
          result = __super::CreateBox(buffer, length);
        }
      }
    }

    CHECK_CONDITION_NOT_NULL_EXECUTE(result, FREE_MEM_CLASS(box));

    if (SUCCEEDED(continueParsing) && (result == NULL))
    {
      result = box;
    }

    CHECK_CONDITION_EXECUTE(FAILED(continueParsing), FREE_MEM_CLASS(box));
  }

  return result;
}

CBox *CMediaInformationBoxFactory::CreateBox(const uint8_t *buffer, uint32_t length)
{
  return NULL;
}