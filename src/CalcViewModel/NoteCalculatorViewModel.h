// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#pragma once

#include "Common/Utils.h"

namespace CalculatorApp
{
    namespace ViewModel
    {
        [Windows::UI::Xaml::Data::Bindable] public ref class NoteCalculatorViewModel sealed : public Windows::UI::Xaml::Data::INotifyPropertyChanged
        {
        public:
            NoteCalculatorViewModel();

            OBSERVABLE_OBJECT_CALLBACK(OnPropertyChanged);

        private:
            void OnPropertyChanged(_In_ Platform::String ^ prop);
        };
    }
}
