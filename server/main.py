from flask import Flask,  request
import json

from queue import Empty, Queue
from threading import Thread
import time
import requests
import traceback
from uuid import uuid4
import os
import random
import cv2
import numpy as np
import math
import pickle

app = Flask(__name__)

@app.route("/")
def home():
    return "Hello, World!"

@app.route("/gameTrackingInitiated", methods = ['POST'])
def gameTrackingInitiated():
    #print("gameTrackingInitiated for uuid :", request.json["header"]["uuid"])

    serverActionQueue.put({
        "action": "gameTrackingInitiated",
        "uuid": request.json["header"]["uuid"],
        "data": request.json,
        "bombModules": request.json["data"]["bombModules"],
    })

    return "ok"

@app.route("/modulesHighlighted", methods = ['POST'])
def modulesHighlighted():
    #print("modulesHighlighted for uuid :", request.json["header"]["uuid"])

    serverActionQueue.put({
        "action": "modulesHighlighted",
        "uuid": request.json["header"]["uuid"],
        "data": request.json, 
    })

    return "ok"

@app.route("/modulesHidden", methods = ['POST'])
def modulesHidden():
    #print("modulesHidden for uuid :", request.json["header"]["uuid"])

    serverActionQueue.put({
        "action": "modulesHidden",
        "uuid": request.json["header"]["uuid"],
        "data": request.json, 
    })


    return "ok"


@app.route("/screenshot/<service_id>/<resource_id>", methods = ['POST'])
def screenshot(service_id, resource_id):
    #print("screenshot for uuid :", service_id, "for resource :", resource_id)

    serverActionQueue.put({
        "action": "screenshot",
        "uuid": service_id,
        "resourceId": resource_id,
        "data": request.json, 
        "fileData": request.data,
    })

    return "ok"

@app.route("/register", methods = ['POST'])
def register():
    #print("service running on :", request.json["data"]["addr"], "with id :", request.json["header"]["uuid"])

    serverActionQueue.put({
        "action": "register",
        "uuid": request.json["header"]["uuid"],
        "addr": request.json["data"]["addr"],
    })

    return "ok"


@app.route("/refreshServersState", methods = ['POST'])
def refreshServersState():
    serverActionQueue.put({
        "action": "refreshServersState",
    })

    return "ok"


# @app.route("/test", methods = ['GET'])
# def test():

#     import requests
#     import time

#     data = {
#         "reset": True,
#     }

#     response = requests.post("http://127.0.0.1:8081/initGameTracking", json=data)
#     print("response=", response.text)

#     if response.headers.get('content-type') == 'application/json':
#         return response.json()
#     else:
#         return response.text

# @app.route("/test2", methods = ['GET'])
# def test2():

#     import requests
#     import time

#     # data = {
#     #     "highlightModules": [
#     #         {
#     #             "face": 0,
#     #             "row": 0,
#     #             "col": 1,
#     #             "color": {
#     #                 "r": 1,
#     #                 "g": 0,
#     #                 "b": 0,
#     #                 "a": 1,
#     #             }
#     #         },
#     #         {
#     #             "face": 0,
#     #             "row": 1,
#     #             "col": 2,
#     #             "color": {
#     #                 "r": 0,
#     #                 "g": 1,
#     #                 "b": 0,
#     #                 "a": 1,
#     #             }
#     #         }
#     #     ],
#     # }
#     modules = []
#     modules.append({
#         "face": 0,
#         "row": 0,
#         "col": 1,
#         "color": {
#             "r": 0,
#             "g": 0,
#             "b": 1,
#             "a": 0,
#         }
#     })
#     modules.append({
#         "face": 0,
#         "row": 1,
#         "col": 0,
#         "color": {
#             "r": 0,
#             "g": 1,
#             "b": 0,
#             "a": 0,
#         }
#     })

#     data = {
#         "highlightModules": modules
#     }

#     response = requests.post("http://127.0.0.1:8081/highlightModules", json=data)
#     print("response=", response.text)

#     if response.headers.get('content-type') == 'application/json':
#         return response.json()
#     else:
#         return response.text

# @app.route("/test3", methods = ['GET'])
# def test3():

#     import requests
#     import time

#     data = {
#         "hideModules": [
#             {
#                 "face": 0,
#                 "row": 0,
#                 "col": 1,
#             },
#             {
#                 "face": 0,
#                 "row": 1,
#                 "col": 0,
#             }
#         ],
#     }

#     response = requests.post("http://127.0.0.1:8081/hideModules", json=data)
#     print("response=", response.text)

#     if response.headers.get('content-type') == 'application/json':
#         return response.json()
#     else:
#         return response.text

# @app.route("/test5", methods = ['GET'])
# def test5():

#     import requests
#     import time

#     data = {
#         "resourceId": "145-hdjue25d-dd585d2d"
#     }

#     response = requests.post("http://127.0.0.1:8081/screenshot", json=data)
#     print("response=", response.text)

#     if response.headers.get('content-type') == 'application/json':
#         return response.json()
#     else:
#         return response.text

# @app.route("/test6", methods = ['GET'])
# def test6():

#     import requests
#     import time

#     data = {
#         "position": {
#             "x": 0.5,
#             "y": 0,
#             "z": 0,
#             "relativeTo": "self",
#         }
#     }

#     response = requests.post("http://127.0.0.1:8081/transform", json=data)
#     print("response=", response.text)

#     if response.headers.get('content-type') == 'application/json':
#         return response.json()
#     else:
#         return response.text


#%%
    

class GameServerManager:

    def __init__(self, modules):
        self.servers = {}
        self.displayServersPool = []
        self.trackingServersPool = []
        self.actions = {
            "display": {},
            "tracking": {},
        }

        self.trackedModules = {}
        for module in modules:
            self.trackedModules[module["name"]] = module
        
        self.dataQueue = Queue()
    
    def registerServer(self, uuid, addr):
        if addr[-1]=="/":
            addr = addr[:-1]
        
        server = {
            "uuid": uuid,
            "addr": addr,
            "type": "display" if (len(self.servers)%2==0) else "tracking"
        }
        self.servers[uuid] = server #thread safe
        
        self.initServer(server)
    
    def __checkResponseError(self, server, response, statusCode=[200], ignoreExceptedError=False):
        
        if response.status_code not in statusCode:
            print(f'Error on initialize server {server["uuid"]} at {server["addr"]} : \n\tstatus code: {response.status_code} \n\t response: {response.text}')
            return None
        
        if response.headers.get('content-type') == 'application/json':
            error = self.__getError(response.json())
            if error is not None:
                if ignoreExceptedError:
                    return (response.json(), "json")
                else:
                    print(f'Error server {server["uuid"]} at {server["addr"]} : \n\tstatus code: {response.status_code} \n\t response: {response.json()}')
                    return None
            else:
                return (response.json(), "json")
        else:
            return (response.text, "text")
        

    def __printError(self, server, response, header=""):
        isJson = False
        if response.headers.get('content-type') == 'application/json':
            isJson = True

        print(f'Error<{header}> on server {server["uuid"]} at {server["addr"]} : \n\tstatus code: {response.status_code} \n\t response: {response.json() if isJson else response.text}')
    
    def __getError(self, data):
        try:
            if data["error"] is True:
                error = {
                    "type": data["errorType"],
                    "message": data["errorMessage"],
                    "stack": data["errorStack"],
                }

                if "errorCode" in data:
                    error["code"] = data["errorCode"]
                else:
                    error["code"] = 0
                
                return error
        except:
            return None
    
    def initServers(self):
        for key, value in self.servers.items():
            self.initServers(value)
    
    def initServer(self, server):
        while True:
            response = requests.post(f'{server["addr"]}/initGameTracking', json={
                "reset": True,
            })

            data = self.__checkResponseError(server, response, statusCode=[200, 403], ignoreExceptedError=True)
            if data is not None:
                data = data[0]
                error = self.__getError(data)
                if error is not None:
                    if error["code"]==1:#bomb not present (mission not started)
                        print(f'Server {server["uuid"]} waiting for mission')
                        time.sleep(10)
                    else:
                        self.__printError(server, response)
                else:
                    break
        
    def initServerCallback(self, uuid, bombModules):
        server = self.servers[uuid]

        if server["type"]=="display":
            self.servers[uuid]["bombInfo"] = {
                "modules": bombModules,
            }
            self.displayServersPool.append(uuid)
        elif server["type"]=="tracking":
            self.trackingServersPool.append(uuid)

    def trackingHighlight(self, uuid, bombModules, resetPreviousTracking=True):
        server = self.servers[uuid]
        modules = []

        for module in bombModules:
            if module["name"] in self.trackedModules:
                moduleColor = self.trackedModules[module["name"]]["color"]
                modules.append({
                    "face": module["gridPosition"]["face"],
                    "row": module["gridPosition"]["row"],
                    "col": module["gridPosition"]["col"],
                    "color": {
                        "r": moduleColor[0],
                        "g": moduleColor[1],
                        "b": moduleColor[2],
                        "a": moduleColor[3],
                    }
                })
        
        response = requests.post(f'{server["addr"]}/highlightModules', json={
            "highlightModules": modules,
        })
        data = self.__checkResponseError(server, response)
        if data is None:
            return False
        
        return True
    
    def modulesTrackingCallback(self, uuid):
        self.dataQueue.put({
            "action": "trackingCallback",
            "uuid": uuid
        })

    def getServerLock(self, type):
        try:
            if type == "display":
                return  self.displayServersPool.pop() # thread safe
            elif type == "tracking":
                return  self.trackingServersPool.pop() # thread safe
        except IndexError:
            return None
        
        return None
    
    def releaseServerLock(self, uuid):
        if uuid not in self.servers:
            return False
        
        server = self.servers[uuid]
        type = server["type"]

        if type == "display":
            self.displayServersPool.append(uuid) # thread safe
        elif type == "tracking":
            self.trackingServersPool.append(uuid) # thread safe
        
        return True
    
    def getServer(self, uuid):
        return self.servers[uuid]
    
    def getNumberAvailableServers(self, type):
        if type=="display":
            return len(self.displayServersPool)
        elif type=="tracking":
            return len(self.trackingServersPool)
        
        return 0
    
    def doRequest(self, uuid, method, url, data=None, raw=False, ignoreExceptedError=False, statusCode=[200]):
        server = self.servers[uuid]
        uri = f'{server["addr"]}/{url}'
        
        response = None

        if method=="POST":
            response = requests.post(uri, json=data)
        elif method=="GET":
            response = requests.get(uri)
        
        if raw:
            return response
        
        return self.__checkResponseError(server, response, statusCode=statusCode, ignoreExceptedError=ignoreExceptedError)

    def doScreenshot(self, uuid, resourceId):
        server = self.servers[uuid]

        response = requests.post(f'{server["addr"]}/screenshot', json={
            "resourceId": resourceId,
        })
        
        data = self.__checkResponseError(server, response)
    
    def screenshotCallback(self, uuid, resourceId, data):
        self.dataQueue.put({
            "action": "processGameEnvImage",
            "uuid": uuid,
            "resourceId": resourceId,
            "data": data,
        })

class ResourceProcessor:
    def __init__(self, minimalContourLength=100, lightAttenuation=111):
        self.minimalContourLength = minimalContourLength
        self.lightAttenuation = lightAttenuation
    
    def process(self, resourceId, type, data, dataType, meta, dataFormat="b"):
        if type=="tracking":

            rawImage = np.asarray(bytearray(data), dtype="uint8")
            rawImage = cv2.imdecode(rawImage, cv2.IMREAD_COLOR) # default is BGR

            vis = np.zeros((rawImage.shape[0], rawImage.shape[1], 3), np.uint8)
            drawingImage = cv2.cvtColor(vis, cv2.COLOR_BGR2RGB)
            processedContours = []

            hsv = cv2.cvtColor(rawImage, cv2.COLOR_BGR2RGB)

            for key, item in meta["trackedModules"].items():
                color = item["color"]
                rgbColor = (math.round(color[0]*255), math.round(color[1]*255), math.round(color[2]*255))
                colorWithoutLight = item["colorWithoutLight"]

                lower_blue = np.array([colorWithoutLight[0], colorWithoutLight[1], colorWithoutLight[2]])
                upper_blue = lower_blue

                mask = cv2.inRange(hsv, lower_blue, upper_blue)

                contours, hierarchy = cv2.findContours(mask, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)

                contourList = []
                hullList = []
                for i in range(len(contours)):
                    contour = contours[i]
                    if cv2.arcLength(contour, True)<=self.minimalContourLength:
                        continue
                    
                    hull = cv2.convexHull(contour, False)
                        
                    contourList.append({
                        "contour": contour,
                        "hull": hull
                    })

                    hullList.append(hull)
                    
                processedContours.append({
                    "name": item["name"],
                    "pickledContours": json.dumps(pickle.dumps(contourList).decode('latin-1')),
                    "pickleCodec": "latin-1",
                })

                cv2.drawContours(drawingImage, hullList, -1, (rgbColor[0], rgbColor[1], rgbColor[2]), 10, 8)

            outputMeta = {
                "contours": processedContours,
            }
            return (cv2.imencode(f'.{dataType}', drawingImage)[1], type, outputMeta)
            

        return (data, type, None)

class StorageManager:
    def __init__(self):
        self.resourcePath = {
            "display": "/home/benjamin/test/display",
            "tracking": "/home/benjamin/test/tracking",
            "trackingProcessed": "/home/benjamin/test/trackingProcessed",
            "metadata": "/home/benjamin/test/metadata",
        }

    def storeResource(self, resourceId, type, data, dataType, dataFormat="b", meta=None):
        filename = None

        if (meta is not None) and ("order" in meta):
            filename = meta["order"]
        else:
            filename = resourceId
        
        with open(os.path.join(self.resourcePath[type], f'{filename}.{dataType}'), f'w{dataFormat}') as file:
            file.write(data)


class TransformManager:

    def __init__(self, gameServerManager, resourceProcessor, storageManager):
        self.gameServerManager = gameServerManager
        self.actions = {}
        self.resourceProcessor = resourceProcessor
        self.storageManager = storageManager
        self.actionServers = {
            "tracking": {},
        }
    
    def transform(self, action):
        if "nextSyncActions" not in action:
            action["resourceId"] = str(uuid4())
            action["nextSyncActions"] = ["display", "tracking"]
            action["nextScreenshots"] = ["display", "tracking"]
            action["servers"] = {
                "display": None,
                "tracking": None,
            }
            action["trackedModules"] = self.gameServerManager.trackedModules

            uuid = self.gameServerManager.getServerLock("display")
            if uuid is not None:
                try:
                    action["servers"]["display"] = uuid

                    data = self.gameServerManager.doRequest(uuid, "POST", "bombTransform", data={
                        "rotation": action["rotation"],
                        "position": action["position"],
                    })

                    data = self.gameServerManager.doRequest(uuid, "GET", "bombTransform")[0]
                    action["bombTransform"] = data["data"]["transform"]

                    moduleGridPos = []
                    for module in self.gameServerManager.getServer(uuid)["bombInfo"]["modules"]:
                        moduleGridPos.append(module["gridPosition"])
                    
                    data = self.gameServerManager.doRequest(uuid, "POST", "modulesBoundingBox", data={
                        "moduleGridPositions": moduleGridPos,
                    })[0]
                    action["modulesBoundingBoxes"] = data["data"]["boundingBoxes"]


                    self.gameServerManager.doScreenshot(uuid, action["resourceId"])

                    action["nextSyncActions"].remove("display")

                    uuid = self.gameServerManager.getServerLock("tracking")
                    if uuid is not None:
                        try:
                            displayServer = self.gameServerManager.getServer(action["servers"]["display"])
                            self.gameServerManager.trackingHighlight(uuid, displayServer["bombInfo"]["modules"], resetPreviousTracking=True)
                            self.actionServers["tracking"][uuid] = action["resourceId"]

                            action["nextSyncActions"].remove("tracking")
                        
                        except Exception as e:
                            self.gameServerManager.releaseServerLock(uuid)
                            action["servers"]["tracking"] = uuid
                            if "tracking" not in action["nextSyncActions"]:
                                action["nextSyncActions"].append("tracking")
                            
                            raise e
                
                except Exception as e:
                    self.gameServerManager.releaseServerLock(uuid)
                    action["servers"]["display"] = None
                    if "display" not in action["nextSyncActions"]:
                        action["nextSyncActions"].append("display")
                    
                    raise e
            
        else:
            
            if "display" in action["nextSyncActions"]:

                uuid = self.gameServerManager.getServerLock("display")
                if uuid is not None:
                    try:
                        action["servers"]["display"] = uuid
                        server = self.gameServerManager.getServer(uuid)

                        data = self.gameServerManager.doRequest(uuid, "POST", "bombTransform", data={
                            "rotation": action["rotation"],
                            "position": action["position"],
                        })

                        moduleGridPos = []
                        for module in server["bombInfo"]["modules"]:
                            moduleGridPos.append(module["gridPosition"])
                        
                        data = self.gameServerManager.doRequest(uuid, "POST", "modulesBoundingBox", data={
                            "moduleGridPositions": moduleGridPos,
                        })[0]
                        action["modulesBoundingBoxes"] = data["data"]["boundingBoxes"]

                        data = self.gameServerManager.doRequest(uuid, "GET", "bombTransform")[0]
                        action["bombTransform"] = data["data"]["transform"]

                        self.gameServerManager.doScreenshot(uuid, action["resourceId"])

                        action["nextSyncActions"].remove("display")
                        
                        
                        uuid = self.gameServerManager.getServerLock("tracking")
                        if uuid is not None:
                            try:
                                displayServer = self.gameServerManager.getServer(action["servers"]["display"])
                                self.gameServerManager.trackingHighlight(uuid, displayServer["bombInfo"]["modules"], resetPreviousTracking=True)
                                self.actionServers["tracking"][uuid] = action["resourceId"]

                                action["nextSyncActions"].remove("tracking")
                            except Exception as e:
                                self.gameServerManager.releaseServerLock(uuid)
                                action["servers"]["tracking"] = uuid
                                if "tracking" not in action["nextSyncActions"]:
                                    action["nextSyncActions"].append("tracking")
                                
                                raise e

                    except Exception as e:
                        self.gameServerManager.releaseServerLock(uuid)
                        action["servers"]["display"] = uuid
                        if "display" not in action["nextSyncActions"]:
                                action["nextSyncActions"].append("display")
                        
                        raise e
                    
            elif "tracking" in action["nextSyncActions"]:
                uuid = self.gameServerManager.getServerLock("tracking")
                if uuid is not None:
                    try:
                        displayServer = self.gameServerManager.getServer(action["servers"]["display"])
                        self.gameServerManager.trackingHighlight(uuid, displayServer["bombInfo"]["modules"], resetPreviousTracking=True)
                        self.actionServers["tracking"][uuid] = action["resourceId"]

                        action["nextSyncActions"].remove("tracking")
                    
                    except Exception as e:
                        self.gameServerManager.releaseServerLock(uuid)
                        action["servers"]["tracking"] = uuid
                        if "tracking" not in action["nextSyncActions"]:
                            action["nextSyncActions"].append("tracking")
                        
                        raise e
        
        
        self.actions[action["resourceId"]] =  action # not optimized

        if len(action["nextSyncActions"])==0:
            return None
        else: 
            return action
    
    def screenshotCallback(self, uuid, resourceId, data):
        try:
            server = self.gameServerManager.getServer(uuid)
            action = self.actions[resourceId]

            (processedData, type, processedMeta) = self.resourceProcessor.process(action["resourceId"], server["type"], data, "png", action, dataFormat="b")
            if processedMeta is not None:
                newMeta = {**action, **processedMeta}
                self.actions[resourceId]["contours"] = processedMeta["contours"]
                self.storageManager.storeResource(action["resourceId"], f'{type}Processed', processedData, "png", dataFormat="b", meta=newMeta)
            
            self.storageManager.storeResource(action["resourceId"], server["type"], data, "png", dataFormat="b", meta=action)

            action["nextScreenshots"].remove(server["type"])

            if len(action["nextScreenshots"])==0:
                self.storageManager.storeResource(action["resourceId"], "metadata", json.dumps(action), "json", dataFormat="")
                del self.actions[resourceId] # not optimized
        finally:
            self.gameServerManager.releaseServerLock(uuid)


    def trackingCallback(self, uuid):
        resourceId = self.actionServers["tracking"][uuid]
        action = self.actions[resourceId]

        data = self.gameServerManager.doRequest(uuid, "POST", "bombTransform", data={
            "rotation": action["rotation"],
            "position": action["position"],
        })
        
        self.gameServerManager.doScreenshot(uuid, resourceId)

        del self.actionServers["tracking"][uuid]


def TransformActionConsumer(gameServerManager, resourceProcessor, storageManager, queue, randomTransform):
    try:
        transformManager = TransformManager(gameServerManager, resourceProcessor, storageManager)

        while (gameServerManager.getNumberAvailableServers("display")==0) or (gameServerManager.getNumberAvailableServers("tracking")==0):
            time.sleep(10)
        
        while True:
            try:
                item = queue.get()
                if item["action"]=="transformGameObject":
                    newItem = transformManager.transform(item)
                    if newItem is not None:
                        queue.put(newItem)
                    
                elif item["action"]=="processGameEnvImage":
                    transformManager.screenshotCallback(item["uuid"], item["resourceId"], item["data"])
                elif item["action"]=="trackingCallback":
                    transformManager.trackingCallback(item["uuid"])
                
                else:
                    #print("[TransformActionConsumer] item action not handled : ", item["action"])
                    pass
                
            except Empty:
                if randomTransform:
                    queue.put({
                        "action": "transformGameObject",
                        "rotation": {
                            "x": random.uniform(0, 360),
                            "y": random.uniform(0, 360),
                            "z": random.uniform(0, 360),
                        }
                    })
                continue
            else:
                #print(f'[TransformActionConsumer] Processing item {item["action"]}')
                queue.task_done()
    except Exception as e:
        print("Exception :", str(e), "trace :", traceback.format_exc())

def ServerActionConsumer(gameServerManager, queue):
    try:
        while True:
            try:
                item = queue.get()
                
                if item["action"]=="register":
                    gameServerManager.registerServer(item["uuid"], item["addr"])
                elif item["action"]=="gameTrackingInitiated":
                    gameServerManager.initServerCallback(item["uuid"], item["bombModules"])
                elif item["action"]=="modulesHighlighted":
                    gameServerManager.modulesTrackingCallback(item["uuid"])
                elif item["action"]=="refreshServersState":
                    gameServerManager.initServers()
                elif item["action"]=="screenshot":
                    gameServerManager.screenshotCallback(item["uuid"], item["resourceId"], item["fileData"])
                else:
                    print("[ServerActionConsumer] item action not handled : ", item["action"])
                #gameTrackingInitiated modulesHighlighted modulesHidden screenshot
                #'/home/benjamin/.steam/steam/steamapps/common/Keep Talking and Nobody Explodes/ktane.x86_64'
            except Empty:
                continue
            else:
                #print(f'[ServerActionConsumer] Processing item {item["action"]}')
                queue.task_done()
    except Exception as e:
        print("Exception :", str(e), "trace :", traceback.format_exc())


#%%

def main():
    modulesToTrack = [
        {
            "name": "TimerComponent(Clone)",
            "color": (1, 0, 0, 1),
            "colorWithoutLight": (150, 0, 0, 255)
        },
        {
            "name": "EmptyComponent(Clone)",
            "color": (0, 1, 0, 1),
            "colorWithoutLight": (0, 144, 0, 255)
        }
    ]

    initTransform = [
        {
            "rotation": {
                "x": 270,
                "y": 180,
                "z": 0,
            }
        },
        {
            "rotation": {
                "x": 275,
                "y": 180,
                "z": 0,
            }
        },
        {
            "rotation": {
                "x": 280,
                "y": 180,
                "z": 0,
            }
        },
        {
            "rotation": {
                "x": 285,
                "y": 180,
                "z": 0,
            }
        },
        {
            "rotation": {
                "x": 290,
                "y": 185,
                "z": 0,
            }
        },
        {
            "rotation": {
                "x": 295,
                "y": 190,
                "z": 0,
            }
        },
        {
            "rotation": {
                "x": 295,
                "y": 195,
                "z": 0,
            }
        },
        {
            "rotation": {
                "x": 295,
                "y": 200,
                "z": 0,
            }
        },
        {
            "rotation": {
                "x": 295,
                "y": 205,
                "z": 0,
            }
        },
        {
            "rotation": {
                "x": 295,
                "y": 210,
                "z": 0,
            }
        },
        {
            "rotation": {
                "x": 295,
                "y": 215,
                "z": 0,
            }
        },
        {
            "rotation": {
                "x": 295,
                "y": 220,
                "z": 0,
            }
        },
        {
            "rotation": {
                "x": 295,
                "y": 225,
                "z": 0,
            }
        },
        {
            "rotation": {
                "x": 295,
                "y": 230,
                "z": 0,
            }
        },
        {
            "rotation": {
                "x": 295,
                "y": 235,
                "z": 0,
            }
        },
        {
            "rotation": {
                "x": 295,
                "y": 240,
                "z": 0,
            }
        },
    ]
    randomTransform = False
    autoOrder = True
    minimalContourLength = 100
    lightAttenuation = 255 - 144

    serverActionQueue = Queue()

    gameServerManager = GameServerManager(modulesToTrack)
    storageManager = StorageManager()
    resourceProcessor = ResourceProcessor(minimalContourLength=minimalContourLength, lightAttenuation=lightAttenuation)


    for i in range(len(initTransform)):
        item = initTransform[i]

        item["action"] = "transformGameObject"

        if "rotation" not in item:
            item["rotation"] = None
        if "position" not in item:
            item["position"] = None

        if autoOrder:
            item["order"] = i
        
        gameServerManager.dataQueue.put(item)
    
    consumer_thread1 = Thread(
        target=ServerActionConsumer,
        args=(gameServerManager, serverActionQueue, ),
        daemon=False
    )
    consumer_thread1.start()

    consumer_thread2 = Thread(
        target=TransformActionConsumer,
        args=(gameServerManager, resourceProcessor, storageManager, gameServerManager.dataQueue, randomTransform, ),
        daemon=False
    )
    consumer_thread2.start()


    #serverActionQueue.join()

    app.run(debug=True)

#%%

if __name__ == "__main__":
    #main()

    FRAME_PER_S = 1
    displayImages = []
    DIR = "/home/benjamin/test/trackingProcessed"
    for file in sorted(os.listdir(DIR), key=lambda x: int(x.split(".")[0])):
        filename = os.fsdecode(file)
        path = os.path.join(DIR, filename)
        print("filename=", filename)

        if not os.path.isfile(path):
            continue
        
        frame = cv2.imread(path)
        displayImages.append(frame)
     

    height,width,layers = displayImages[0].shape

    fourcc = cv2.VideoWriter_fourcc(*'mp4v') 
    video = cv2.VideoWriter('/home/benjamin/test/video.avi', fourcc, 5, (width, height))


    for image in displayImages:
        video.write(image)

    cv2.destroyAllWindows()
    video.release()