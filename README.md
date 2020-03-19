# FixMatchingEngine

This is a toy FIX matching engine that can be used for development of FIX client programs (i.e initiators). 
This engine only supports limit-GoddTillCancel orders at the moment. Engine does not reject your requests with TimeInForceValues other than GTC
but it assumes them to be GTC.

There is no symbol control, engine creates a new market for a new symbol that is arriving with requests.

You can see this engine as a basic matcher and no other controls.
