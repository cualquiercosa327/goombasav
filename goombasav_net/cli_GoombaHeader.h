#pragma once
/* cli_GoombaHeader.h - class to encapsulate a particular header within the GoombaSRAM object

This object will only remain valid while its GoombaSRAM has not yet been
disposed/finalized. GoombaHeader objects can be obtained via the Headers
property of GoombaSRAM.

Copyright (C) 2014 libertyernie

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 2 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program. If not, see <http://www.gnu.org/licenses/>.

https://github.com/libertyernie/goombasav */

#include "../goombasav.h"

using System::String;

namespace Goombasav {
	ref class GoombaSRAM;

	public ref class GoombaHeader abstract {
	private:
		const void* ptr;
		GoombaSRAM^ parent;
	internal:
		// Constructs an object using the given header pointer and parent object.
		// The parent is only used when the user tries to access the Parent property.
		GoombaHeader(const void* ptr, GoombaSRAM^ parent) {
			this->parent = parent;
			this->ptr = (const stateheader*)ptr;
		}

		bool IsPlausible() {
			return stateheader_plausible((const stateheader*)ptr);
		}
	public:
		static uint16_t STATESAVE = GOOMBA_STATESAVE;
		static uint16_t SRAMSAVE = GOOMBA_SRAMSAVE;
		static uint16_t CONFIGSAVE = GOOMBA_CONFIGSAVE;
		static uint32_t STATEID = GOOMBA_STATEID;

#pragma region properties
		property const void* VoidPointer {
			const void* get() {
				return (const void*)this->ptr;
			}
		}

		property GoombaSRAM^ Parent {
			GoombaSRAM^ get() {
				return this->parent;
			}
		}

		property uint16_t Size {
			uint16_t get() {
				return ((const stateheader*)ptr)->size;
			}
		}

		property uint16_t Type {
			uint16_t get() {
				return ((const stateheader*)ptr)->type;
			}
		}

		property String^ Title {
			String^ get() {
				return gcnew String(((const stateheader*)ptr)->title);
			}
		}
#pragma endregion

		String^ GetSummary() {
			return gcnew String(stateheader_summary_str((const stateheader*)ptr));
		}

		String^ GetDescription() {
			return gcnew String(stateheader_str((const stateheader*)ptr));
		}

		String^ ToString() override {
			return GetSummary();
		}
	};
}