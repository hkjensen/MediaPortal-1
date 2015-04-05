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

#include "SampleToChunkBox.h"
#include "BoxCollection.h"

CSampleToChunkBox::CSampleToChunkBox(HRESULT *result)
  : CFullBox(result)
{
  this->type = NULL;
  this->samplesToChunks = NULL;

  if ((result != NULL) && (SUCCEEDED(*result)))
  {
    this->type = Duplicate(SAMPLE_TO_CHUNK_BOX_TYPE);
    this->samplesToChunks = new CSampleToChunkCollection(result);

    CHECK_POINTER_HRESULT(*result, this->type, *result, E_OUTOFMEMORY);
    CHECK_POINTER_HRESULT(*result, this->samplesToChunks, *result, E_OUTOFMEMORY);
  }
}

CSampleToChunkBox::~CSampleToChunkBox(void)
{
  FREE_MEM_CLASS(this->samplesToChunks);
}

/* get methods */

CSampleToChunkCollection *CSampleToChunkBox::GetSamplesToChunks(void)
{
  return this->samplesToChunks;
}

/* set methods */

/* other methods */

wchar_t *CSampleToChunkBox::GetParsedHumanReadable(const wchar_t *indent)
{
  wchar_t *result = NULL;
  wchar_t *previousResult = __super::GetParsedHumanReadable(indent);

  if ((previousResult != NULL) && (this->IsParsed()))
  {
    // prepare sample entries collection
    wchar_t *samplesToChunks = NULL;
    wchar_t *tempIndent = FormatString(L"%s\t", indent);
    for (unsigned int i = 0; i < this->GetSamplesToChunks()->Count(); i++)
    {
      CSampleToChunk *sampleToChunk = this->GetSamplesToChunks()->GetItem(i);
      wchar_t *tempSamplesToChunks = FormatString(
        L"%s%s%sFirst chunk: %5u Samples per chunk: %3u Sample description index: %2u",
        (i == 0) ? L"" : samplesToChunks,
        (i == 0) ? L"" : L"\n",
        tempIndent,
        sampleToChunk->GetFirstChunk(),
        sampleToChunk->GetSamplesPerChunk(),
        sampleToChunk->GetSampleDescriptionIndex());
      FREE_MEM(samplesToChunks);

      samplesToChunks = tempSamplesToChunks;
    }

    // prepare finally human readable representation
    result = FormatString(
      L"%s\n" \
      L"%sChunk offsets:%s" \
      L"%s"
      ,
      
      previousResult,
      indent, (this->GetSamplesToChunks()->Count() == 0) ? L"" : L"\n",
      (this->GetSamplesToChunks()->Count() == 0) ? L"" : samplesToChunks

      );

    FREE_MEM(samplesToChunks);
    FREE_MEM(tempIndent);
  }

  FREE_MEM(previousResult);

  return result;
}

uint64_t CSampleToChunkBox::GetBoxSize(void)
{
  uint64_t result = 4 + this->GetSamplesToChunks()->Count() * 12;

  if (result != 0)
  {
    uint64_t boxSize = __super::GetBoxSize();
    result = (boxSize != 0) ? (result + boxSize) : 0; 
  }

  return result;
}

bool CSampleToChunkBox::ParseInternal(const unsigned char *buffer, uint32_t length, bool processAdditionalBoxes)
{
  this->samplesToChunks->Clear();

  if (__super::ParseInternal(buffer, length, false))
  {
    this->flags &= ~BOX_FLAG_PARSED;
    this->flags |= (wcscmp(this->type, SAMPLE_TO_CHUNK_BOX_TYPE) == 0) ? BOX_FLAG_PARSED : BOX_FLAG_NONE;

    if (this->IsSetFlags(BOX_FLAG_PARSED))
    {
      // box is sample to chunk box, parse all values
      uint32_t position = this->HasExtendedHeader() ? FULL_BOX_HEADER_LENGTH_SIZE64 : FULL_BOX_HEADER_LENGTH;
      HRESULT continueParsing = (this->GetSize() <= (uint64_t)length) ? S_OK : E_NOT_VALID_STATE;

      if (SUCCEEDED(continueParsing))
      {
        RBE32INC_DEFINE(buffer, position, entryCount, uint32_t);

        CHECK_CONDITION_HRESULT(continueParsing, this->samplesToChunks->EnsureEnoughSpace(entryCount), continueParsing, E_OUTOFMEMORY);

        for (uint32_t i = 0; (SUCCEEDED(continueParsing) && (i < entryCount)); i++)
        {
          CSampleToChunk *sampleToChunk = new CSampleToChunk(&continueParsing);
          CHECK_POINTER_HRESULT(continueParsing, sampleToChunk, continueParsing, E_OUTOFMEMORY);

          if (SUCCEEDED(continueParsing))
          {
            sampleToChunk->SetFirstChunk(RBE32(buffer, position));
            position += 4;

            sampleToChunk->SetSamplesPerChunk(RBE32(buffer, position));
            position += 4;

            sampleToChunk->SetSampleDescriptionIndex(RBE32(buffer, position));
            position += 4;
          }

          CHECK_CONDITION_HRESULT(continueParsing, this->samplesToChunks->Add(sampleToChunk), continueParsing, E_OUTOFMEMORY);
          CHECK_CONDITION_EXECUTE(FAILED(continueParsing), FREE_MEM_CLASS(sampleToChunk));
        }
      }

      if (SUCCEEDED(continueParsing) && processAdditionalBoxes)
      {
        this->ProcessAdditionalBoxes(buffer, length, position);
      }

      this->flags &= ~BOX_FLAG_PARSED;
      this->flags |= SUCCEEDED(continueParsing) ? BOX_FLAG_PARSED : BOX_FLAG_NONE;
    }
  }

  return this->IsSetFlags(BOX_FLAG_PARSED);
}

uint32_t CSampleToChunkBox::GetBoxInternal(uint8_t *buffer, uint32_t length, bool processAdditionalBoxes)
{
  uint32_t result = __super::GetBoxInternal(buffer, length, false);

  if (result != 0)
  {
    WBE32INC(buffer, result, this->GetSamplesToChunks()->Count());

    for (uint32_t i = 0; (i < this->GetSamplesToChunks()->Count()); i++)
    {
      CSampleToChunk *sampleToChunk = this->GetSamplesToChunks()->GetItem(i);
      WBE32INC(buffer, result, sampleToChunk->GetFirstChunk());
      WBE32INC(buffer, result, sampleToChunk->GetSamplesPerChunk());
      WBE32INC(buffer, result, sampleToChunk->GetSampleDescriptionIndex());
    }

    if ((result != 0) && processAdditionalBoxes && (this->GetBoxes()->Count() != 0))
    {
      uint32_t boxSizes = this->GetAdditionalBoxes(buffer + result, length - result);
      result = (boxSizes != 0) ? (result + boxSizes) : 0;
    }
  }

  return result;
}