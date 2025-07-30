# SoS Construction Site Simulation

## Overview

This repository contains the implementation of an **Intelligent System of Systems (SoS)** to simulate, monitor, and optimize operations on modern construction sites. The project integrates Unity-based simulations, real-time data from IoT devices, and an MQTT communication system to emulate and analyze construction workflows effectively.

> **Portfolio Project**: This is a public portfolio version of an academic project demonstrating advanced system integration, 3D simulation, and IoT communication technologies.

## Key Technologies & Skills Demonstrated

- **Unity 3D Development**: Advanced C# scripting, behavior trees, and real-time simulation
- **IoT Integration**: MQTT protocol implementation for device communication
- **System Architecture**: Microservices design with Node-RED flows
- **Real-time Data Processing**: Live monitoring and control systems
- **3D Visualization**: Complex terrain generation and interactive UI design
- **Software Integration**: Blender  Unity pipeline for 3D asset management

## Features

- **3D Unity Simulation**: Models realistic construction site operations, including actor interactions and task execution.  
- **Scratch-like Task Definition**: Intuitive drag-and-drop interface for defining simple and complex tasks.  
- **Real-time Monitoring**: Live updates on machine positions and statuses using MQTT.  
- **Configurable Construction Sites**: Tools for creating zones, roads, and assigning tasks.  
- **Resource Management**: Management of machines, workers, and constellations (grouped resources).  
- **Data Analysis and Logging**: Insights into simulated workflows with exportable logs for further analysis.

## Project Structure

- **Unity Simulation**: Core Unity project files, including 3D models, behavior trees, and scripts for machine control.  
- **MQTT Communication**: Integration with an EMQX MQTT broker for bidirectional communication.  
- **Configuration Tools**: Components for defining zones, gates, roads, and operational parameters.  
- **Node-RED Flows**: Simulations of real-site hardware using modular MQTT nodes.

## Technical Highlights

### Architecture
- **Unity Simulator**: Backend behavior logic in C# using behavior trees for machine autonomy
- **MQTT Communication**: Real-time data exchange using lightweight JSON messages
- **Hardware Simulation**: Node-RED flows simulate hardware, ensuring seamless integration with Unity

### Advanced Features
- Dynamic terrain generation from real-world data (BlenderGIS integration)
- Configurable construction site layouts with zone management
- Real-time machine coordination and task scheduling
- Interactive 3D visualization with intuitive controls

## Installation

As the Construction Site project is a desktop app using the Unity engine, the deployment is quite simple. All you need to do is clone the GitHub repository and open the project in the Unity editor. There you need to go to File build settings

After opening the build settings a new window will pop up. There you need to set the target platform and the architecture then click build.

After pressing build you will need to choose the destination and then the project will start building, after that, you will have an executable file from which you can run the program with other necessary files in the same folder.

### Prerequisites

- **Unity Engine**: Version 2022.3.47f1
- **Node.js & Node-RED**: For hardware simulation
- **EMQX Broker**: Hosted MQTT broker for message routing
- **.NET Framework**: For Unity's MQTTNet library integration

### Steps

1. Clone this repository
2. Open the Unity project in Unity Hub
3. Install Node-RED and import the provided JSON flow for site simulation
4. Set up the EMQX MQTT broker with the provided configuration details
5. Run the Unity simulation and connect it to the broker for real-time monitoring

## Usage

### General Interface

There are several Pages in which the User can interact to edit the details of the entities it will be able to manage.

#### Manage Entities

This UI page will contain three other main pages that allow the user to add Machines, Workers and Create/Edit Constellations.

#### Open Construction Site

After opening the page it will be possible to view all the previously created construction sites. To see additional details it is possible to hover with the mouse over the site and to open them there is the "Open" button that allows for an eagle eye view of the site with all the created zones.

#### Create Construction Site

The creation of construction sites involves a step-by-step process to define zones, gates, and roads with the help of intuitive tools within the Unity-based application.

1. **Defining Zones**  
   - Use the Zone Manager to outline a zone by selecting points on the construction site
   - The Snap Manager ensures accurate point selection by snapping to valid positions along the perimeter
   - Once all points are selected, tools like PolygonUtils validate the zone's geometry, checking for errors like self-intersections or invalid angles
   - The Renderer Manager provides a clear visual representation of the completed zone

2. **Adding Gates**  
   - Switch to gate creation mode in the Construction Site Manager
   - Select a segment of the zone perimeter to place a gate, with Snap Manager assisting in precise positioning
   - The Renderer Manager updates the visual representation of the zone with the new gate

3. **Creating Roads**  
   - The Road Manager enables the placement of road paths connecting gates, edges, or nodes
   - Snap Manager ensures logical and accessible road layouts by dynamically snapping road points to appropriate locations
   - Roads are rendered in real-time, providing immediate feedback on the construction site's layout

### Simulation

1. Open the Unity application
2. Create a construction site by defining zones, roads, and gates
3. First assign constellations (spawns the machines) to zones with the specific button, then add tasks to them using the drag-and-drop task builder
4. Start the simulation to visualize operations and analyze performance

### Real-time Monitoring

1. Set up Node-RED to simulate real hardware operations
2. Configure the Unity platform to connect to the MQTT broker by opening a construction site and clicking on the specific button to connect to the "real construction site"
3. Monitor live data and control site operations through Unity

## 3D Map Creation Guide

This guide outlines the steps to create a detailed 3D map using BlenderGIS, add buildings, and import the map into Unity for the construction site simulation project.

### Setup and Requirements

#### Tools Needed
- **Blender (Version 4.x or later)** - [Download Blender](https://www.blender.org/download/)
- **BlenderGIS Addon** - [Installation Guide](https://github.com/domlysz/BlenderGIS)
- **Unity** - [Download Unity Hub](https://unity.com/download)
- **Object to Terrain Script** - *(Included in the project)*

### Step 1: Create the Map in BlenderGIS

1. **Search for the Location**
   - Open Blender and enable the BlenderGIS addon
   - Start the basemap with the * key to open the map source setup dialog
   - Navigate using mouse controls and keyboard shortcuts

2. **Generate the Terrain**
   - Export the current map view as a textured plane mesh
   - Use **Get SRTM** button to fetch elevation data from OpenTopography

3. **Add Buildings**
   - Import 3D building data from OpenStreetMap
   - Configure height settings and elevation mapping

4. **Export the Map**
   - Export as FBX file for Unity import

### Step 2: Import into Unity

1. Import the FBX file into Unity's Assets folder
2. Configure the model and place in scene
3. Convert terrain mesh to Unity terrain using the Object to Terrain tool
4. Apply terrain texturing and add props as needed

## Future Enhancements

- Support for more advanced analytics using ETL pipelines
- Scalable MQTT architecture with data persistence
- Enhanced real-time performance with edge processing
- Machine learning integration for predictive analytics

## Contributors

- Federico Albertini  
- Sebastian Perea Lopez  
- Andrea Carbonetti  
- Aleksa Savković  
- Somoy Barua  
- Tommaso Pasini  
- Momir Savić

---

*This project demonstrates the integration of multiple technologies to create a comprehensive simulation environment for construction site management and optimization.*
