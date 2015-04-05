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

#ifndef __NOTE_SOURCE_DESCRIPTION_ITEM_DEFINED
#define __NOTE_SOURCE_DESCRIPTION_ITEM_DEFINED

#include "SourceDescriptionItem.h"

#define NOTE_SOURCE_DESCRIPTION_ITEM_TYPE                             0x07

class CNoteSourceDescriptionItem : public CSourceDescriptionItem
{
public:
  // initializes a new instance of CNoteSourceDescriptionItem class
  CNoteSourceDescriptionItem(HRESULT *result);
  virtual ~CNoteSourceDescriptionItem(void);

  /* get methods */

  // gets note source description item type
  // @return : note source description item type
  virtual unsigned int GetType(void);

  // gets note source description item size
  // @return : note source description item size
  virtual unsigned int GetSize(void);

  // get whole note source description item into buffer
  // @param buffer : the buffer to store note source description item
  // @param length : the length of buffer
  // @return : true if successful, false otherwise
  virtual bool GetSourceDescriptionItem(unsigned char *buffer, unsigned int length);

  // gets note
  // @return : note or NULL if error
  virtual const wchar_t *GetNote(void);

  /* set methods */

  // sets note
  // @param note : the note to set
  // @return : true if successful, false otherwise
  virtual bool SetNote(const wchar_t *note);

  /* other methods */

  // sets current instance to default state
  virtual void Clear(void);

  // parses data in buffer
  // @param buffer : buffer with source description item data for parsing
  // @param length : the length of data in buffer
  // @return : true if successfully parsed, false otherwise
  virtual bool Parse(const unsigned char *buffer, unsigned int length);

protected:

  // holds note
  wchar_t *note;
};

#endif