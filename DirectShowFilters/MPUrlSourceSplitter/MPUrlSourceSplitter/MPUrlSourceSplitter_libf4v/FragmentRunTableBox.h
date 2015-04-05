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

#ifndef __FRAGMENT_RUN_TABLE_BOX_DEFINED
#define __SEGMENT_RUN_TABLE_BOX_DEFINED

#include "FullBox.h"

#include "QualitySegmentUrlModifierCollection.h"
#include "FragmentRunEntryCollection.h"

#define FRAGMENT_RUN_TABLE_BOX_TYPE                                               L"afrt"

#define FRAGMENT_RUN_TABLE_BOX_END_OF_PRESENTATION                                0x00000000
#define FRAGMENT_RUN_TABLE_BOX_DISCONTINUITY_FRAGMENT_NUMBERING                   0x00000001
#define FRAGMENT_RUN_TABLE_BOX_DISCONTINUITY_TIMESTAMPS                           0x00000002
#define FRAGMENT_RUN_TABLE_BOX_DISCONTINUITY_FRAGMENT_NUMBERING_AND_TIMESTAMPS    0x00000003

class CFragmentRunTableBox :
  public CFullBox
{
public:
  // initializes a new instance of CFragmentRunTableBox class
  CFragmentRunTableBox(HRESULT *result);

  // destructor
  virtual ~CFragmentRunTableBox(void);

  // gets box data in human readable format
  // @param indent : string to insert before each line
  // @return : box data in human readable format or NULL if error
  virtual wchar_t *GetParsedHumanReadable(const wchar_t *indent);

  // gets the number of time units per second
  // @return : the number of time units per second
  virtual uint32_t GetTimeScale(void);

  // gets quality segment url modifiers table
  // @return : quality segment url modifiers table
  virtual CQualitySegmentUrlModifierCollection *GetQualitySegmentUrlModifiers(void);

  // gets fragment run entry table
  // @return : fragment run entry table
  virtual CFragmentRunEntryCollection *GetFragmentRunEntryTable(void);

protected:
  uint32_t timeScale;

  CQualitySegmentUrlModifierCollection *qualitySegmentUrlModifiers;
  CFragmentRunEntryCollection *fragmentRunEntryTable;

  // parses data in buffer
  // @param buffer : buffer with box data for parsing
  // @param length : the length of data in buffer
  // @param processAdditionalBoxes : specifies if additional boxes have to be processed
  // @return : true if parsed successfully, false otherwise
  virtual bool ParseInternal(const unsigned char *buffer, uint32_t length, bool processAdditionalBoxes);
};

#endif