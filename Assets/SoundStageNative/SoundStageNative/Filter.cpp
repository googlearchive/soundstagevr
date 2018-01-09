// Copyright 2017 Google LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#include "main.h"
#include "Filter.h"

extern "C" {

	float ProcessSample(FilterData* fd, float sample)
	{
		float input = sample - fd->q * fd->b4;//feedback

		float t1 = fd->b1;
		fd->b1 = (input + fd->b0) * fd->p - fd->b1 * fd->f;
		float t2 = fd->b2; fd->b2 = (fd->b1 + t1) * fd->p - fd->b2 * fd->f;
		t1 = fd->b3;
		fd->b3 = (fd->b2 + t2) * fd->p - fd->b3 * fd->f;
		fd->b4 = (fd->b3 + t1) * fd->p - fd->b4 * fd->f;
		fd->b4 = fd->b4 - fd->b4 * fd->b4 * fd->b4 * 0.166667f; //clipping
		fd->b0 = input;

		if (fd->LP) return fd->b4;
		else return input - fd->b4;
	}

	void processStereoFilter(float buffer[], int length, FilterData* mfA, FilterData* mfB)
	{
		for (int i = 0; i < length; i += 2)
		{
			buffer[i] = ProcessSample(mfA, buffer[i]); 
			buffer[i + 1] = ProcessSample(mfB, buffer[i+1]);
		}
	}
}