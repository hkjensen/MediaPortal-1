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

#ifndef __PROGRAM_SPECIFIC_INFORMATION_PACKET_DEFINED
#define __PROGRAM_SPECIFIC_INFORMATION_PACKET_DEFINED

#include "TsPacket.h"
#include "SectionPayloadCollection.h"
#include "TsPacketCollection.h"

#define PROGRAM_SPECIFIC_INFORMATION_PACKET_FLAG_NONE                       TS_PACKET_FLAG_NONE

#define PROGRAM_SPECIFIC_INFORMATION_PACKET_FLAG_WRITTEN_POINTER_FIELD      (1 << (TS_PACKET_FLAG_LAST + 0))

#define PROGRAM_SPECIFIC_INFORMATION_PACKET_FLAG_LAST                       (TS_PACKET_FLAG_LAST + 1)


class CSection;

class CProgramSpecificInformationPacket : public CTsPacket
{
public:
  CProgramSpecificInformationPacket(HRESULT *result, uint16_t pid, uint8_t tableId);
  CProgramSpecificInformationPacket(HRESULT *result, uint16_t pid, uint8_t tableId, bool reference);
  virtual ~CProgramSpecificInformationPacket(void);

  /* get methods */

  // gets section payloads
  // @return : section payloads
  CSectionPayloadCollection *GetSectionPayloads(void);

  // gets table ID of section
  // @return : table ID of section
  uint8_t GetTableId(void);

  /* set methods */

  /* other methods */

  // parses data in buffer
  // @param buffer : buffer with MPEG2 TS data for parsing
  // @param length : the length of data in buffer
  // @return : true if parsed successfully, false otherwise
  virtual bool Parse(const unsigned char *buffer, uint32_t length);

  // parses section data into PSI packet
  // @param sectionData : the section data to parse
  // @param sectionDataSize : the section data size
  // @param sectionStart : specifies if section starts
  // @param fillStuffingBytes : specifies if stuffing bytes have to be written
  // @param processedDataSize : the reference to variable holding processed section data size
  // @return : S_OK if successful, error code otherwise
  virtual HRESULT ParseSectionData(const uint8_t *sectionData, unsigned int sectionDataSize, bool sectionStart, bool fillStuffingBytes, unsigned int *processedDataSize);

  // tests if PROGRAM_SPECIFIC_INFORMATION_PACKET_FLAG_WRITTEN_POINTER_FIELD flag is set
  // @return : true if flag is set, false otherwise
  bool IsWrittenPointerField(void);

protected:
  // holds MPEG2 TS packet PID, which is specified for program specific information packet (e.g. 0x0000 for PAT, 0x0001 for CAT, etc.)
  uint16_t pid;
  // the section table ID to split content of PSI packet into section payloads
  uint8_t tableId;
  // holds section payloads
  CSectionPayloadCollection *sectionPayloads;

  /* methods */

  // gets new instance of MPEG2 TS packet
  // @return : new MPEG2 TS packet instance or NULL if error
  virtual CTsPacket *CreateItem(void);

  // deeply clones current instance
  // @param item : the MPEG2 TS packet instance to clone
  // @return : true if successful, false otherwise
  virtual bool InternalClone(CTsPacket *item);
};

#endif