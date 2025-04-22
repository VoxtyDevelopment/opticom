local lightTimers = {}
local debugCooldowns = {}

function isWhitelistedVehicle(vehicle)
    if not Config.UseWhitelist then return true end
    local modelHash = GetEntityModel(vehicle)
    local modelName = GetDisplayNameFromVehicleModel(modelHash)
    for _, allowed in ipairs(Config.WhitelistedVehicles) do
        if modelName == allowed then return true end
    end
    return false
end

function setLightGreen(light)
    SetEntityTrafficlightOverride(light, 0)
    stopNearbyTraffic(light)
    lightTimers[light] = SetTimeout(5000, function()
        resetTrafficLight(light)
    end)

    local coords = GetEntityCoords(light)
    local streetHash, crossingHash = GetStreetNameAtCoord(coords.x, coords.y, coords.z)
    local streetName = GetStreetNameFromHashKey(streetHash)
    local crossingName = crossingHash ~= 0 and GetStreetNameFromHashKey(crossingHash) or nil

    local streetInfo = crossingName and (streetName .. " & " .. crossingName) or streetName
    local msg = ("Traffic light set to green at %s"):format(streetInfo)
    debugPrint(msg, light)
end

function resetTrafficLight(light)
    SetEntityTrafficlightOverride(light, -1)
    lightTimers[light] = nil
end

function syncLight(light, isGreen)
    TriggerServerEvent('opticom:syncLights', light, isGreen)
end

function debugPrint(msg, id)
    if not Config.Debug then return end
    local currentTime = GetGameTimer()
    local key = id or "global"

    if not debugCooldowns[key] or currentTime - debugCooldowns[key] > 5000 then
        print("[Opticom Debug] " .. msg)
        debugCooldowns[key] = currentTime
    end
end

function getForwardPosition(position, heading, distance)
    local rad = math.rad(heading)
    return vector3(position.x - distance * math.sin(rad), position.y + distance * math.cos(rad), position.z)
end

function stopNearbyTraffic(light)
    local lightPos = GetEntityCoords(light)
    local radius = 50.0
    local vehicles = {}

    for i = 1, 50 do
        local offset = GetOffsetFromEntityInWorldCoords(light, math.random(-radius, radius), math.random(-radius, radius), 0.0)
        local veh = GetClosestVehicle(offset.x, offset.y, offset.z, 10.0, 0, 70)
        if veh and veh ~= 0 and not IsPedAPlayer(GetPedInVehicleSeat(veh, -1)) then
            if not vehicles[veh] then
                TaskVehicleTempAction(GetPedInVehicleSeat(veh, -1), veh, 27, 5000)
                vehicles[veh] = true
            end
        end
    end
end

Citizen.CreateThread(function()
    while true do
        local player = GetPlayerPed(-1)

        if IsPedInAnyVehicle(player, false) then
            local vehicle = GetVehiclePedIsIn(player, false)

            if IsVehicleSirenOn(vehicle) and isWhitelistedVehicle(vehicle) then
                local pos = GetEntityCoords(player)
                local heading = GetEntityHeading(player)
                local foundLight = nil
                local pollDelay = 50

                for dist = 30.0, 5.0, -10.0 do
                    Citizen.Wait(pollDelay)

                    local checkPos = getForwardPosition(pos, heading, dist)

                    for _, objectType in pairs(Config.TrafficLightObjects) do
                        local light = GetClosestObjectOfType(checkPos, 20.0, objectType, false, false, false)

                        if light ~= 0 then
                            local lightHeading = GetEntityHeading(light)
                            local headingDiff = math.abs(heading - lightHeading)

                            if headingDiff < 40.0 or headingDiff > 320.0 then
                                setLightGreen(light, GetPlayerName(PlayerId()))
                                syncLight(light, true)
                                foundLight = light
                                break
                            end
                        end
                    end

                    if foundLight then
                        local ratio = (dist - 5.0) / (30.0 - 5.0)
                        pollDelay = math.max(50, math.floor(50 - ratio * (50 - 50)))
                        break
                    end
                end
            else
                Citizen.Wait(1000)
            end
        else
            Citizen.Wait(1000)
        end
    end
end)