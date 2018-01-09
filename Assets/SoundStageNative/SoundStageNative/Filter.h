/*
 * Copyright 2017 Google LLC
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#pragma once

//#include <string.h> // for memset

struct FilterData
{
	float f, p, q; //filter coefficients
	float b0, b1, b2, b3, b4; //filter buffers (beware denormals!)
	bool LP;
};



extern "C"
{
	//filter
	SOUNDSTAGE_API void processStereoFilter(float buffer[], int length, FilterData* mfA, FilterData* mfB);
}



