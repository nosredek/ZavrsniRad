<template>
  <div>
    <button class="button"  @click="goBack">Back</button>
    <button class="button"  @click="histogramsView">Histograms view</button>
    <div class="row">
      <select @change="zoom" v-model="currentZoom">
        <option :value="1">100%</option>
        <option :value="2">200%</option>
        <option :value="5">500%</option>
        <option :value="10">1000%</option>
      </select>
      <select @change="changeReference" v-model="currentReference">
        <template v-for="(ref,index) in references">
          <option :key="index" :selected="index==0" :value="ref.name">{{ref.name}}</option>
        </template>
      </select>
      <label>Position:</label>
      <input type="number" @change="move" v-model="currentPos" />

      <div class="level-right">
        <div class="level-item" style="display:inline-block">
          <p>
            Loaded {{currentImageChunk}}/{{chunks}} image chunks
            <font-awesome-icon v-if="currentImageChunk!=chunks" icon="spinner" spin />
            <font-awesome-icon v-else icon="check" />
          </p>
          <p>
            Loaded {{currentHistogramChunk}}/{{chunks}} histogram chunks
            <font-awesome-icon v-if="currentHistogramChunk!=chunks" icon="spinner" spin />
            <font-awesome-icon v-else icon="check" />
          </p>
        </div>
      </div>
    </div>

    <div id="ruler" style="position:sticky;background:white;top:0px;z-index:200;"></div>
    <div id="histogram" style="position:sticky;background:white;top:130px;z-index:100;"></div>
    <div id="image" style="margin-top:10px;margin-left:5px"></div>
  </div>
</template>

<script lang="ts">
import { Component, Vue } from "vue-property-decorator";

import VueKonva from "vue-konva";
import Konva from "konva";
import axios, { CancelToken, CancelTokenSource } from "axios";

import { SamFile } from "@/types/bioTypes";
import { FastLayer } from "konva/types/FastLayer";
import { Rect } from "konva/types/shapes/Rect";
import { Node } from "konva/types/Node";
import { ReferenceModel } from "@/store/types";

Vue.use(VueKonva);

@Component({
  components: {}
})
export default class Visualization extends Vue {
  currentZoom: number = 1;
  currentPos: number = 0;

  currentReference: string = "";
  references: Array<ReferenceModel> = [];
  cancelToken?: CancelTokenSource = undefined;

  currentImageChunk: number = 0;
  currentHistogramChunk: number = 0;
  chunks: number = 0;

  goBack(): void {
    this.cancelToken!.cancel("Operation canceled");
    this.cancelToken = axios.CancelToken.source();

    this.$router.push({ name: "home" });
  }

  histogramsView(): void {
    this.cancelToken!.cancel("Operation canceled");
    this.cancelToken = axios.CancelToken.source();

    this.$router.push({ name: "histograms" });
  }

  async refreshImages(): Promise<void> {
    this.currentImageChunk = 0;
    this.currentHistogramChunk = 0;
    let chunks = await axios.post(
      `/main/chunks`,
      { referenceName: this.currentReference, zoom: this.currentZoom },
      { cancelToken: this.cancelToken!.token }
    );
    this.chunks = chunks.data;
    this.histogramStage!.clearCache();
    this.histogramStage!.destroyChildren();

    this.imageStage!.clearCache();
    this.imageStage!.destroyChildren();

    this.tooltips();
    this.histogram(this.currentZoom);
    this.image(this.currentZoom);
  }

  zoom(): void {
    this.cancelToken!.cancel("Operation canceled");
    this.cancelToken = axios.CancelToken.source();
    this.currentPos = 0;
    this.move();

    this.refreshImages();
  }

  move(): void {
    let x = -this.currentPos * this.currentZoom;
    let node = this.imageStage!.find(".imageLayer");
    if (node) {
      node[0].x(x);
      (<any>node[0]).batchDraw();
    }
    node = this.histogramStage!.find(".histoLayer");
    if (node) {
      node[0].x(x);
      (<any>node[0]).batchDraw();
    }
    node = this.imageStage!.find(".helpLayer");
    if (node) {
      node[0].x(x);
      (<any>node[0]).batchDraw();
    }
    node = this.rulerStage!.find(".ruler");

    let scale =
      (window.innerWidth - 25) /
      this.references.find(i => i.name == this.currentReference)!.length;

    if (node) {
      node[0].x(this.currentPos * scale);
      node = this.rulerStage!.find(".rulerLayer");
      (<any>node[0]).batchDraw();
    }
  }

  changeReference(): void {
    this.cancelToken!.cancel("Operation canceled");
    this.cancelToken = axios.CancelToken.source();
    this.currentZoom = 1;
    this.currentPos = 0;
    this.move();

    this.rulerStage!.clearCache();
    this.rulerStage!.destroyChildren();

    this.initHistogram().then(() => {
      this.scaledHistogram();
      this.refreshImages();
    });
  }

  histogramStage: Konva.Stage | null = null;
  imageStage: Konva.Stage | null = null;
  rulerStage: Konva.Stage | null = null;
  mounted() {
    this.cancelToken = axios.CancelToken.source();
    this.histogramStage = new Konva.Stage({
      container: "histogram",
      width: window.innerWidth,
      height: 220,
      draggable: false
    });
    this.rulerStage = new Konva.Stage({
      container: "ruler",
      width: window.innerWidth,
      height: 130,
      draggable: false
    });
    this.imageStage = new Konva.Stage({
      container: "image",
      width: window.innerWidth,
      height: window.innerHeight,
      draggable: false
    });
    let refs = this.$store.getters["references"];
    this.currentReference = refs[0].name;
    this.references = refs;

    this.initHistogram().then(async () => {
      let chunks = await axios.post(
        `/main/chunks`,
        { referenceName: this.currentReference, zoom: this.currentZoom },
        { cancelToken: this.cancelToken!.token }
      );
      this.tooltips();
      this.chunks = chunks.data;
      this.scaledHistogram();
      this.histogram(this.currentZoom);
      this.image(this.currentZoom);
    });

    window.addEventListener("resize", () => {
      this.histogramStage!.width(window.innerWidth);
      this.rulerStage!.width(window.innerWidth);
      this.imageStage!.width(window.innerWidth);
      this.rulerStage!.clearCache();
      this.rulerStage!.destroyChildren();
      this.scaledHistogram();
    });
  }

  async tooltips(): Promise<void> {
    var tooltips = await axios.post(`/main/tooltips`, {
      referenceName: this.currentReference,
      zoom: this.currentZoom
    });
    let helpLayer: any = new Konva.Layer({ name: "helpLayer" });
    let tooltipLayer: any = new Konva.Layer();

    var tooltip = new Konva.Text({
      text: "",
      fontFamily: "Calibri",
      fontSize: 20,
      padding: 5,
      visible: false,
      fill: "lightgreen",
      opacity: 1,
      textFill: "white"
    });

    tooltipLayer.add(tooltip);

    for (let index = 0; index < tooltips.data.length; index++) {
      const element = tooltips.data[index];
      helpLayer.add(
        new Konva.Rect({
          x: element.x,
          y: element.y,
          width: element.width,
          height: element.height,
          name: element.text
        })
      );
    }

    this.imageStage!.add(tooltipLayer);
    this.imageStage!.add(helpLayer);

    helpLayer.on("mousemove", (e: any) => {
      var mousePos = this.imageStage!.getPointerPosition();
      tooltip.position({
        x: mousePos!.x + 5,
        y: mousePos!.y + 5
      });
      tooltip.text(e.target.name());
      tooltip.show();
      tooltipLayer.draw();
    });

    helpLayer.on("mouseout", () => {
      tooltip.hide();
      tooltipLayer.draw();
    });
  }

  async initHistogram(): Promise<void> {
    return await axios.post(`/main/calculateHist`, {
      referenceName: this.currentReference
    });
  }

  async histogram(zoom: number): Promise<void> {
    let layer: any = new Konva.Layer({
      draggable: true,
      dragBoundFunc: pos => {
        let newX = pos.x > 0 ? 0 : pos.x;
        return {
          x: newX,
          y: layer.absolutePosition().y
        };
      },
      name: "histoLayer"
    });

    layer.on("dragmove", (eventTarget: MouseEvent) => {
      let x = (<any>eventTarget!.target!).attrs.x;
      this.currentPos = Math.round(-x / this.currentZoom);

      let node = this.imageStage!.find(".imageLayer");
      if (node) {
        node[0].x(x);
        (<any>node[0]).batchDraw();
      }

      node = this.imageStage!.find(".helpLayer");
      if (node) {
        node[0].x(x);
        (<any>node[0]).batchDraw();
      }
      let scale =
        (window.innerWidth - 25) /
        this.references.find(i => i.name == this.currentReference)!.length;

      node = this.rulerStage!.find(".ruler");
      if (node) {
        node[0].x((-x * scale) / this.currentZoom);
        (<any>node[0]).draw();
        node = this.rulerStage!.find(".rulerLayer");
        (<any>node[0]).batchDraw();
      }
    });

    this.histogramStage!.add(layer);
    for (let index = 0; index < this.chunks; index++) {
      await axios
        .post(
          `/main/histogram`,
          {
            referenceName: this.currentReference,
            zoom: zoom,
            chunk: index
          },
          { cancelToken: this.cancelToken!.token }
        )
        .then((result: any) => {
          let img = new Image();

          img.onload = () => {
            var kImg = new Konva.Image({
              image: img,
              x: index * 15000,
              y: 0,
              width: index < this.chunks - 1 ? 15000 : undefined
            });

            layer.add(kImg);
            layer.batchDraw();
          };
          img.src = "data:image/webp;base64," + result.data;
        });
      this.currentHistogramChunk = index + 1;
    }
  }

  async scaledHistogram(): Promise<void> {
    let layer: any = new Konva.Layer({
      name: "scaledHistoLayer"
    });

    let ruler: any = new Konva.Rect({
      y: 90,
      width: 10,
      height: 20,
      opacity: 70,
      fill: "orange",
      stroke: "orange",
      draggable: true,
      dragBoundFunc: pos => {
        let newX = pos.x < 0 ? 0 : pos.x;
        newX = newX > window.innerWidth - 25 ? window.innerWidth - 25 : newX;
        return {
          x: newX,
          y: ruler.absolutePosition().y
        };
      },
      name: "ruler"
    });

    let rulerLayer: any = new Konva.Layer({
      name: "rulerLayer"
    });

    ruler.on("dragmove", (eventTarget: MouseEvent) => {
      let scale =
        (window.innerWidth - 25) /
        this.references.find(i => i.name == this.currentReference)!.length;

      let x = -((<any>eventTarget!.target!).attrs.x * this.currentZoom) / scale;
      this.currentPos = Math.round(-x / this.currentZoom);
      let node = this.imageStage!.find(".imageLayer");
      if (node) {
        node[0].x(x);
        (<any>node[0]).batchDraw();
      }
      node = this.histogramStage!.find(".histoLayer");
      if (node) {
        node[0].x(x);
        (<any>node[0]).batchDraw();
      }
      node = this.imageStage!.find(".helpLayer");
      if (node) {
        node[0].x(x);
        (<any>node[0]).batchDraw();
      }
    });
    rulerLayer.add(ruler);

    this.rulerStage!.add(layer);
    this.rulerStage!.add(rulerLayer);
    await axios
      .post(
        `/main/scaledHistogram`,
        {
          referenceName: this.currentReference,
          screenSize: window.innerWidth - 25
        },
        { cancelToken: this.cancelToken!.token }
      )
      .then((result: any) => {
        let img = new Image();

        img.onload = () => {
          var kImg = new Konva.Image({
            image: img,
            x: 0,
            y: 0
          });

          layer.add(kImg);
          layer.batchDraw();
        };
        img.src = "data:image/png;base64," + result.data;
      });
  }

  async image(zoom: number): Promise<void> {
    let layer: any = new Konva.Layer({
      draggable: false,
      name: "imageLayer"
    });

    this.imageStage!.add(layer);

    for (let index = 0; index < this.chunks; index++) {
      await axios
        .post(
          `/main/image`,
          {
            referenceName: this.currentReference,
            zoom: zoom,
            chunk: index
          },
          { cancelToken: this.cancelToken!.token }
        )
        .then((result: any) => {
          let img = new Image();

          img.onload = () => {
            var kImg = new Konva.Image({
              image: img,
              x: index * 15000,
              y: 0,
              width: index < this.chunks - 1 ? 15000 : undefined
            });

            if (index == 7) {
              console.log(img);
              console.log(img.width);
            }
            layer.add(kImg);
            layer.batchDraw();
          };
          img.src = "data:image/webp;base64," + result.data;
        });
      try {
        if (
          layer.children.some(
            (el: Node) => el.height() > this.imageStage!.height()
          )
        ) {
          let max = Math.max.apply(
            Math,
            layer.children.map((el: Node) => {
              return el.height();
            })
          );
          console.log(max);
          this.imageStage!.height(max);
        }
      } catch {}
      this.currentImageChunk = index + 1;
    }
  }
}
</script>
