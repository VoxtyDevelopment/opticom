RegisterServerEvent('opticom:syncLights')
AddEventHandler('opticom:syncLights', function(trafficLight, isGreen)
    TriggerClientEvent('opticom:syncLights', -1, trafficLight, isGreen)
end)