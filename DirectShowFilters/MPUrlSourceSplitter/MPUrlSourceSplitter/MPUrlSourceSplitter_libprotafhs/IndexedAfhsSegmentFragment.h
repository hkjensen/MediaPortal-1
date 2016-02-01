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

#ifndef __INDEXED_AFHS_SEGMENT_FRAGMENT_DEFINED
#define __INDEXED_AFHS_SEGMENT_FRAGMENT_DEFINED

#include "IndexedStreamFragment.h"
#include "AfhsSegmentFragment.h"

#define INDEXED_AFHS_SEGMENT_FRAGMENT_FLAG_NONE                       INDEXED_STREAM_FRAGMENT_FLAG_NONE

#define INDEXED_AFHS_SEGMENT_FRAGMENT_FLAG_LAST                       (INDEXED_STREAM_FRAGMENT_FLAG_LAST + 0)

class CIndexedAfhsSegmentFragment : public CIndexedStreamFragment
{
public:
  CIndexedAfhsSegmentFragment(HRESULT *result, CAfhsSegmentFragment *item, unsigned int index);
  virtual ~CIndexedAfhsSegmentFragment(void);

  /* get methods */

  // gets AFHS segment fragment
  // @return : AFHS segment fragment
  virtual CAfhsSegmentFragment *GetItem(void);

  /* set methods */

  /* other methods */

protected:

  /* methods */
};

#endif