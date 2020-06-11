<template>
  <div>
    <button class="button" @click="goBack">Back</button>
    <p>
      {{ isLoading? "Loading" : "Loaded"}}
      <font-awesome-icon v-if="isLoading" icon="spinner" spin />
      <font-awesome-icon v-else icon="check" />
    </p>
    <div id="histograms"></div>
  </div>
</template>

<script lang="ts">
import { Component, Vue } from "vue-property-decorator";

import Konva from "konva";

import { ReferenceModel } from "@/store/types";
import axios, { CancelToken, CancelTokenSource } from "axios";

@Component({
  components: {}
})
export default class Visualization extends Vue {
  histogramStage: Konva.Stage | null = null;
  cancelToken?: CancelTokenSource = undefined;
  layer: Konva.Layer | null = null;

  isLoading = true;

  goBack(): void {
    this.cancelToken!.cancel("Operation canceled");
    this.cancelToken = axios.CancelToken.source();

    this.$router.push({ name: "visualization" });
  }

  async mounted() {
    this.cancelToken = axios.CancelToken.source();

    let refs: Array<ReferenceModel> = this.$store.getters["references"];
    this.histogramStage = new Konva.Stage({
      container: "histograms",
      width: window.innerWidth,
      height: 120 * refs.length,
      draggable: false
    });

    this.layer = new Konva.Layer({
      name: "scaledHistoLayer"
    });
    this.histogramStage.add(this.layer);

    for (let index = 0; index < refs.length; index++) {
      const element = refs[index];
      await axios.post(`/main/calculateHist`, {
        referenceName: element.name
      });
      await this.scaledHistogram(index, element.name);
    }
    this.isLoading = false;
  }

  async scaledHistogram(index: number, reference: string): Promise<void> {
    await axios
      .post(
        `/main/scaledHistogram`,
        {
          referenceName: reference,
          screenSize: window.innerWidth - 25
        },
        { cancelToken: this.cancelToken!.token }
      )
      .then((result: any) => {
        let img = new Image();

        img.onload = () => {
          var kImg = new Konva.Image({
            image: img,
            x: 5,
            y: index * 120
          });

          this.layer!.add(kImg);
          this.layer!.batchDraw();
        };
        img.src = "data:image/png;base64," + result.data;
        this.layer!.add(
          new Konva.Text({
            text: "Reference name: " + reference,
            x: 20,
            y: 120 * index + 101
          })
        );
      });
  }
}
</script>

<style>
</style>