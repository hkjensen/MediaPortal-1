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

#include "stdafx.h"

#include "Mpeg2tsStreamFragmentContext.h"

CMpeg2tsStreamFragmentContext::CMpeg2tsStreamFragmentContext(HRESULT *result, CMpeg2tsStreamFragment *fragment)
  : CFlags()
{
  this->fragment = NULL;
  this->packets = NULL;

  if ((result != NULL) && (SUCCEEDED(*result)))
  {
    CHECK_POINTER_DEFAULT_HRESULT(*result, fragment);

    this->packets = new CTsPacketContextCollection(result);

    CHECK_POINTER_HRESULT(*result, this->packets, *result, E_OUTOFMEMORY);
    CHECK_CONDITION_EXECUTE(SUCCEEDED(*result), this->fragment = fragment);
  }
}

CMpeg2tsStreamFragmentContext::~CMpeg2tsStreamFragmentContext()
{
  FREE_MEM_CLASS(this->packets);
}

/* get methods */

CMpeg2tsStreamFragment *CMpeg2tsStreamFragmentContext::GetFragment(void)
{
  return this->fragment;
}

CTsPacketContextCollection *CMpeg2tsStreamFragmentContext::GetPacketContexts(void)
{
  return this->packets;
}

/* set methods */

/* other methods */

/* protected methods */
