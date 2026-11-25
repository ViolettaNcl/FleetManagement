# Multi-vehicle dispatch lab

The independent synthetic scenario generates 1–40 delivery orders and 1–8 vehicles with common capacity. The depot is at (50,50), travel uses Euclidean distance at 60 km/h, and all vehicles share an 08:00–16:00 shift. Each order has integer demand, service duration and earliest/latest service-start time. A vehicle departs once, does not reload, and must return before shift end.

Two constructive heuristics: first feasible append in order-ID order; cheapest feasible insertion in deadline order, minimizing incremental route distance among feasible insertion positions. Each candidate is evaluated by the same schedule function including travel, waiting, capacity, service start deadlines and return to depot. The recommended candidate serves the most orders among these two, then has the shorter total distance. It is not guaranteed globally optimal. Unassigned means no placement found by that heuristic, not mathematically infeasible.

UI: route overlay, vehicle loads, per-vehicle Gantt service blocks, detailed arrival/start/end table and explicit unassigned list. Time labels are rounded to the nearest minute; feasibility uses unrounded values. No traffic, road-network costs, driver assignment, breaks, pickup/delivery precedence, actual fleet mutation or persistence. Future work: constrained local search, OR-Tools server comparison, actual calendar constraints and human-approved plan application.

All scenarios and computed metrics are synthetic and reproducible with the seed. Tests cover exact order accounting, load/time/shift constraints, waiting, impossible requests and deterministic recommendation.
