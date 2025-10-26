using System.Collections.Generic;
using Spider_Man.Webshooter.Gadgets.WebNet;
using ThunderRoad;
using UnityEngine;

namespace Spider_Man.Webshooter.Gadgets.WebBall
{
    public class HitItemWithWeb : MonoBehaviour
    {
        private bool stuck;
        private void OnCollisionEnter(Collision other)
        {
            if (!stuck)
            {
                stuck = true;
                this.gameObject.GetComponent<Item>().physicBody.rigidBody.isKinematic = true;
                Item.OnItemDespawn += item => { Destroy(this);};
                var spawnPoint = other.GetContact(0).point + (-other.GetContact(0).normal * 0.2f);
                Catalog.InstantiateAsync("webNet" as object, spawnPoint,
                    this.gameObject.transform.rotation, null,
                    callback =>
                    {
                        callback.transform.LookAt(this.gameObject.transform.position);
                        callback.transform.localScale *= 0.3f;
                        var item = callback.GetComponent<Item>();
                        var center = item.GetCustomReference("Center");
                        List<GameObject> nodes = new List<GameObject>();
                        foreach (var go in item.customReferences)
                        {
                            foreach (var go2 in item.customReferences)
                            {
                                if (!go2.Equals(go))
                                {
                                    Physics.IgnoreCollision(go.transform.gameObject.GetComponent<Collider>(),
                                        go2.transform.gameObject.GetComponent<Collider>());
                                }
                            }

                            go.transform.gameObject.GetComponent<Rigidbody>().useGravity = true;
                            if (!go.name.Equals("Center"))
                            {
                                nodes.Add(go.transform.gameObject);
                            }

                            switch (go.name)
                            {
                                case "TopMiddle":
                                    var list = new List<GameObject>();
                                    list.Add(item.GetCustomReference("TopLeft").transform.gameObject);
                                    go.transform.gameObject.AddComponent<WebConnector>().Setup(list);
                                    break;
                                case "TopRight":
                                    var list1 = new List<GameObject>();
                                    list1.Add(item.GetCustomReference("TopMiddle").transform.gameObject);
                                    go.transform.gameObject.AddComponent<WebConnector>().Setup(list1);
                                    break;
                                case "TopLeft":
                                    var list2 = new List<GameObject>();
                                    list2.Add(item.GetCustomReference("CenterLeft").transform.gameObject);
                                    go.transform.gameObject.AddComponent<WebConnector>().Setup(list2);
                                    break;
                                case "CenterRight":
                                    var list3 = new List<GameObject>();
                                    list3.Add(item.GetCustomReference("TopRight").transform.gameObject);
                                    go.transform.gameObject.AddComponent<WebConnector>().Setup(list3);
                                    break;
                                case "CenterLeft":
                                    var list4 = new List<GameObject>();
                                    list4.Add(item.GetCustomReference("BottomLeft").transform.gameObject);
                                    go.transform.gameObject.AddComponent<WebConnector>().Setup(list4);
                                    break;
                                case "BottomMiddle":
                                    var list5 = new List<GameObject>();
                                    list5.Add(item.GetCustomReference("BottomRight").transform.gameObject);
                                    go.transform.gameObject.AddComponent<WebConnector>().Setup(list5);
                                    break;
                                case "BottomLeft":
                                    var list6 = new List<GameObject>();
                                    list6.Add(item.GetCustomReference("BottomMiddle").transform.gameObject);
                                    go.transform.gameObject.AddComponent<WebConnector>().Setup(list6);
                                    break;
                                case "BottomRight":
                                    var list7 = new List<GameObject>();
                                    list7.Add(item.GetCustomReference("CenterRight").transform.gameObject);
                                    go.transform.gameObject.AddComponent<WebConnector>().Setup(list7);
                                    break;
                                case "Ring1TopRight":
                                    var list8 = new List<GameObject>();
                                    list8.Add(item.GetCustomReference("Ring1TopMiddle").transform.gameObject);
                                    list8.Add(item.GetCustomReference("Ring2TopRight").transform.gameObject);
                                    go.transform.gameObject.AddComponent<WebConnector>().Setup(list8);
                                    break;
                                case "Ring2TopRight":
                                    var list9 = new List<GameObject>();
                                    list9.Add(item.GetCustomReference("Ring2TopMiddle").transform.gameObject);
                                    list9.Add(item.GetCustomReference("TopRight").transform.gameObject);
                                    go.transform.gameObject.AddComponent<WebConnector>().Setup(list9);
                                    break;
                                case "Ring1TopLeft":
                                    var list10 = new List<GameObject>();
                                    list10.Add(item.GetCustomReference("Ring1CenterLeft").transform.gameObject);
                                    list10.Add(item.GetCustomReference("Ring2TopLeft").transform.gameObject);
                                    go.transform.gameObject.AddComponent<WebConnector>().Setup(list10);
                                    break;
                                case "Ring2TopLeft":
                                    var list11 = new List<GameObject>();
                                    list11.Add(item.GetCustomReference("Ring2CenterLeft").transform.gameObject);
                                    list11.Add(item.GetCustomReference("TopLeft").transform.gameObject);
                                    go.transform.gameObject.AddComponent<WebConnector>().Setup(list11);
                                    break;
                                case "Ring1TopMiddle":
                                    var list12 = new List<GameObject>();
                                    list12.Add(item.GetCustomReference("Ring1TopLeft").transform.gameObject);
                                    list12.Add(item.GetCustomReference("Ring2TopMiddle").transform.gameObject);
                                    go.transform.gameObject.AddComponent<WebConnector>().Setup(list12);
                                    break;
                                case "Ring2TopMiddle":
                                    var list13 = new List<GameObject>();
                                    list13.Add(item.GetCustomReference("Ring2TopLeft").transform.gameObject);
                                    list13.Add(item.GetCustomReference("TopMiddle").transform.gameObject);
                                    go.transform.gameObject.AddComponent<WebConnector>().Setup(list13);
                                    break;
                                case "Ring1BottomRight":
                                    var list14 = new List<GameObject>();
                                    list14.Add(item.GetCustomReference("Ring1CenterRight").transform.gameObject);
                                    list14.Add(item.GetCustomReference("Ring2BottomRight").transform.gameObject);
                                    go.transform.gameObject.AddComponent<WebConnector>().Setup(list14);
                                    break;
                                case "Ring1BottomLeft":
                                    var list15 = new List<GameObject>();
                                    list15.Add(item.GetCustomReference("Ring1BottomMiddle").transform.gameObject);
                                    list15.Add(item.GetCustomReference("Ring2BottomLeft").transform.gameObject);
                                    go.transform.gameObject.AddComponent<WebConnector>().Setup(list15);
                                    break;
                                case "Ring2BottomRight":
                                    var list16 = new List<GameObject>();
                                    list16.Add(item.GetCustomReference("Ring2CenterRight").transform.gameObject);
                                    list16.Add(item.GetCustomReference("BottomRight").transform.gameObject);
                                    go.transform.gameObject.AddComponent<WebConnector>().Setup(list16);
                                    break;
                                case "Ring2BottomLeft":
                                    var list17 = new List<GameObject>();
                                    list17.Add(item.GetCustomReference("Ring2BottomMiddle").transform.gameObject);
                                    list17.Add(item.GetCustomReference("BottomLeft").transform.gameObject);
                                    go.transform.gameObject.AddComponent<WebConnector>().Setup(list17);
                                    break;
                                case "Ring1BottomMiddle":
                                    var list18 = new List<GameObject>();
                                    list18.Add(item.GetCustomReference("Ring1BottomRight").transform.gameObject);
                                    list18.Add(item.GetCustomReference("Ring2BottomMiddle").transform.gameObject);
                                    go.transform.gameObject.AddComponent<WebConnector>().Setup(list18);
                                    break;
                                case "Ring2BottomMiddle":
                                    var list19 = new List<GameObject>();
                                    list19.Add(item.GetCustomReference("Ring2BottomRight").transform.gameObject);
                                    list19.Add(item.GetCustomReference("BottomMiddle").transform.gameObject);
                                    go.transform.gameObject.AddComponent<WebConnector>().Setup(list19);
                                    break;
                                case "Ring1CenterRight":
                                    var list20 = new List<GameObject>();
                                    list20.Add(item.GetCustomReference("Ring1TopRight").transform.gameObject);
                                    list20.Add(item.GetCustomReference("Ring2CenterRight").transform.gameObject);
                                    go.transform.gameObject.AddComponent<WebConnector>().Setup(list20);
                                    break;
                                case "Ring2CenterRight":
                                    var list21 = new List<GameObject>();
                                    list21.Add(item.GetCustomReference("Ring2TopRight").transform.gameObject);
                                    list21.Add(item.GetCustomReference("CenterRight").transform.gameObject);
                                    go.transform.gameObject.AddComponent<WebConnector>().Setup(list21);
                                    break;
                                case "Ring1CenterLeft":
                                    var list22 = new List<GameObject>();
                                    list22.Add(item.GetCustomReference("Ring1BottomLeft").transform.gameObject);
                                    list22.Add(item.GetCustomReference("Ring2CenterLeft").transform.gameObject);
                                    go.transform.gameObject.AddComponent<WebConnector>().Setup(list22);
                                    break;
                                case "Ring2CenterLeft":
                                    var list23 = new List<GameObject>();
                                    list23.Add(item.GetCustomReference("Ring2BottomLeft").transform.gameObject);
                                    list23.Add(item.GetCustomReference("CenterLeft").transform.gameObject);
                                    go.transform.gameObject.AddComponent<WebConnector>().Setup(list23);
                                    break;
                                default:
                                    break;
                            }

                        }

                        center.gameObject.AddComponent<WebConnector>().Setup(nodes, other.GetContact(0).normal);
                    }, "WebCreatureHandler");
            }
        }
    }
}